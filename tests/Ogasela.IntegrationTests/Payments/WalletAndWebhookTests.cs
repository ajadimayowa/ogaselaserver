using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Ogasela.Application.Payments.FundWallet;
using Ogasela.Application.Payments.GetTransactions;
using Ogasela.Application.Payments.GetWallet;
using Ogasela.Api.Contracts.Payments;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Payments;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Payments;

public class WalletAndWebhookTests : IClassFixture<PaymentsApiFactory>
{
    private readonly PaymentsApiFactory _factory;

    public WalletAndWebhookTests(PaymentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FundWallet_ThenWebhookConfirms_IncreasesBalance()
    {
        var client = await RegisterSellerAsync(_factory);

        var fundResponse = await client.PostAsJsonAsync("/api/v1/wallet/fund", new FundWalletRequest(500_000m));
        fundResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var funded = await fundResponse.Content.ReadFromJsonAsync<FundWalletResponse>(JsonOptions);

        var webhookResponse = await SendPaystackWebhookAsync(funded!.GatewayReference, validSignature: true);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallet = await GetWalletAsync(client);
        wallet.BalanceKobo.Should().Be(500_000m);
    }

    [Fact]
    public async Task DuplicateWebhookDelivery_DoesNotDoubleCreditTheWallet()
    {
        var client = await RegisterSellerAsync(_factory);

        var fundResponse = await client.PostAsJsonAsync("/api/v1/wallet/fund", new FundWalletRequest(200_000m));
        var funded = await fundResponse.Content.ReadFromJsonAsync<FundWalletResponse>(JsonOptions);

        var firstDelivery = await SendPaystackWebhookAsync(funded!.GatewayReference, validSignature: true);
        firstDelivery.StatusCode.Should().Be(HttpStatusCode.OK);

        // The gateway (or a retrying proxy in front of it) redelivers the exact same webhook.
        var secondDelivery = await SendPaystackWebhookAsync(funded.GatewayReference, validSignature: true);
        secondDelivery.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallet = await GetWalletAsync(client);
        wallet.BalanceKobo.Should().Be(200_000m, "a redelivered webhook must not credit the wallet a second time");
    }

    [Fact]
    public async Task InvalidWebhookSignature_IsRejectedWithoutAlteringTheWallet()
    {
        var client = await RegisterSellerAsync(_factory);

        var fundResponse = await client.PostAsJsonAsync("/api/v1/wallet/fund", new FundWalletRequest(300_000m));
        var funded = await fundResponse.Content.ReadFromJsonAsync<FundWalletResponse>(JsonOptions);

        var webhookResponse = await SendPaystackWebhookAsync(funded!.GatewayReference, validSignature: false);

        webhookResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wallet = await GetWalletAsync(client);
        wallet.BalanceKobo.Should().Be(0m, "an unverifiable webhook must never move money");
    }

    [Fact]
    public async Task PublishingAPaidListing_DebitsTheWalletAtomically_AndFailsCleanlyOnInsufficientBalance()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);

        const decimal basicPlanPriceKobo = 75_000m;
        await FundAndConfirmAsync(client, basicPlanPriceKobo);

        var firstListing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, PromotionPlanSeedData.BasicId);
        var firstPublish = await PublishAsync(client, firstListing.Id);
        firstPublish.StatusCode.Should().Be(HttpStatusCode.OK);

        var walletAfterFirstPurchase = await GetWalletAsync(client);
        walletAfterFirstPurchase.BalanceKobo.Should().Be(0m);

        var transactions = await GetTransactionsAsync(client);
        transactions.Items.Should().ContainSingle(t =>
            t.Type == TransactionType.PlanPurchase
            && t.Status == TransactionStatus.Success
            && t.AmountKobo == basicPlanPriceKobo
            && t.RelatedListingId == firstListing.Id);

        // A second paid listing with no remaining balance must fail cleanly: no partial debit,
        // the listing stays a Draft.
        var secondListing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, PromotionPlanSeedData.BasicId);
        var secondPublish = await PublishAsync(client, secondListing.Id);
        secondPublish.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

        var walletAfterFailedPurchase = await GetWalletAsync(client);
        walletAfterFailedPurchase.BalanceKobo.Should().Be(0m, "a failed payment must never debit the wallet");

        var getSecondListingResponse = await client.GetAsync($"/api/v1/listings/{secondListing.Id}");
        var reloadedSecondListing = await getSecondListingResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        reloadedSecondListing!.Status.Should().Be(ListingStatus.Draft, "the listing must never publish when payment fails");
    }

    private async Task FundAndConfirmAsync(HttpClient client, decimal amountKobo)
    {
        var fundResponse = await client.PostAsJsonAsync("/api/v1/wallet/fund", new FundWalletRequest(amountKobo));
        var funded = await fundResponse.Content.ReadFromJsonAsync<FundWalletResponse>(JsonOptions);

        var webhookResponse = await SendPaystackWebhookAsync(funded!.GatewayReference, validSignature: true);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> SendPaystackWebhookAsync(string reference, bool validSignature)
    {
        var body = FakePaymentGateway.BuildWebhookBody(reference);
        var signature = validSignature ? FakePaymentGateway.ComputeSignature(body) : "not-a-real-signature";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/paystack")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-paystack-signature", signature);

        var anonymousClient = _factory.CreateClient();
        return await anonymousClient.SendAsync(request);
    }

    private static async Task<WalletResponse> GetWalletAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/wallet");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<WalletResponse>(JsonOptions))!;
    }

    private static async Task<PagedTransactionsResponse> GetTransactionsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedTransactionsResponse>(JsonOptions))!;
    }
}
