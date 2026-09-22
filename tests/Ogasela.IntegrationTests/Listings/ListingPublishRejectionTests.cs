using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Infrastructure.Listings;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

/// <summary>
/// Covers every RULE-01..06 rejection path plus repost's re-enforcement of the same rules, and
/// the hourly expiry job. Uses the default <see cref="ListingsApiFactory"/> - Free-plan
/// scenarios never touch IPaymentAuthorizer, and the payment-failure scenario relies on the
/// real StubPaymentAuthorizer always failing paid plans, so no fake payment authorizer is needed here.
/// </summary>
public class ListingPublishRejectionTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public ListingPublishRejectionTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Publish_WhenSellerIsNotVerified_IsRejected()
    {
        var client = await RegisterSellerAsync(_factory);
        var listing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, PromotionPlanSeedData.FreeId);

        var response = await PublishAsync(client, listing.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Listing.NotVerified");
    }

    [Fact]
    public async Task Publish_WithFreePlanInANonFreeEligibleCategory_IsRejected()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(client, CategorySeedData.VehiclesId, PromotionPlanSeedData.FreeId);

        var response = await PublishAsync(client, listing.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Listing.CategoryNotFreeEligible");
    }

    [Fact]
    public async Task Publish_WithFreePlanAfterTheSellerAlreadyHasThreeActiveFreeListings_IsRejected()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);

        for (var i = 0; i < 3; i++)
        {
            await CreateAndPublishFreeListingAsync(client, CategorySeedData.ElectronicsId);
        }

        var fourthListing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, PromotionPlanSeedData.FreeId);
        var response = await PublishAsync(client, fourthListing.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Listing.FreePlanCapReached");
    }

    [Fact]
    public async Task Publish_WithAPaidPlanWhenPaymentIsNotAuthorized_IsRejectedAndNeverActivated()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, PromotionPlanSeedData.BasicId);

        var response = await PublishAsync(client, listing.Id);

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Listing.PaymentFailed");

        var getResponse = await client.GetAsync($"/api/v1/listings/{listing.Id}");
        var reloaded = await getResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        reloaded!.Status.Should().Be(ListingStatus.Draft, "a failed payment must never publish the listing");
    }

    [Fact]
    public async Task Repost_WhenTheFreePlanCapIsReached_IsRejectedTheSameWayAFreshPublishWouldBe()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);

        // Publish listing A first (while no other Free listing is active), then sell it so it
        // no longer counts toward the cap, before filling the cap with three fresh listings.
        var listingA = await CreateAndPublishFreeListingAsync(client, CategorySeedData.ElectronicsId);
        (await MarkSoldAsync(client, listingA.Id)).EnsureSuccessStatusCode();

        for (var i = 0; i < 3; i++)
        {
            await CreateAndPublishFreeListingAsync(client, CategorySeedData.ElectronicsId);
        }

        // Now three other Free listings are Active (cap reached); reposting the sold listing
        // must re-run RULE-05 and be rejected exactly like a fresh publish would be.
        var repostResponse = await RepostAsync(client, listingA.Id);

        repostResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await repostResponse.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Listing.FreePlanCapReached");
    }

    [Fact]
    public async Task Repost_OfAnExpiredFreeListing_SucceedsAndReactivatesWithANewExpiryDate()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(client, CategorySeedData.ElectronicsId);

        await BackdateExpiryAndRunExpiryJobAsync(listing.Id);

        var repostResponse = await RepostAsync(client, listing.Id);
        repostResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reposted = await repostResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        reposted!.Status.Should().Be(ListingStatus.Active);
        reposted.PublishedAt.Should().NotBeNull();
        (reposted.ExpiresAt!.Value - reposted.PublishedAt!.Value).Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task ExpiryJob_TransitionsAnActiveListingToExpired_OnceExpiresAtHasPassed()
    {
        var client = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(client, CategorySeedData.ElectronicsId);

        await BackdateExpiryAndRunExpiryJobAsync(listing.Id);

        var getResponse = await client.GetAsync($"/api/v1/listings/{listing.Id}");
        var reloaded = await getResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        reloaded!.Status.Should().Be(ListingStatus.Expired);
    }

    /// <summary>
    /// Backdates the listing's ExpiresAt via the change tracker (bypassing the private setter,
    /// the same way EF itself would populate it from a row) so the expiry job has something to
    /// act on immediately, instead of waiting for the plan's real multi-day duration to elapse.
    /// </summary>
    private async Task BackdateExpiryAndRunExpiryJobAsync(Guid listingId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var listing = await dbContext.Listings.FirstAsync(l => l.Id == listingId);
        dbContext.Entry(listing).Property(l => l.ExpiresAt).CurrentValue = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var job = scope.ServiceProvider.GetRequiredService<ListingExpiryJob>();
        await job.RunAsync();
    }

    private sealed record ProblemDetailsResponse(string Title, string Detail);
}
