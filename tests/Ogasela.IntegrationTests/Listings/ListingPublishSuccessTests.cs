using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

/// <summary>
/// Uses <see cref="PaidPlanListingsApiFactory"/> (payment always authorized) so paid plans can
/// actually reach Active - the default factory's real StubPaymentAuthorizer always fails paid
/// plans by design (see ListingPublishRejectionTests for that path).
/// </summary>
public class ListingPublishSuccessTests : IClassFixture<PaidPlanListingsApiFactory>
{
    private readonly PaidPlanListingsApiFactory _factory;

    public ListingPublishSuccessTests(PaidPlanListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Free", 7)]
    [InlineData("Basic", 15)]
    [InlineData("Standard", 21)]
    [InlineData("Premium", 30)]
    public async Task Publish_ForEachPromotionPlan_SucceedsWithExpiresAtSetToPublishedAtPlusPlanDuration(
        string planName, int expectedDurationDays)
    {
        var planId = planName switch
        {
            "Free" => PromotionPlanSeedData.FreeId,
            "Basic" => PromotionPlanSeedData.BasicId,
            "Standard" => PromotionPlanSeedData.StandardId,
            "Premium" => PromotionPlanSeedData.PremiumId,
            _ => throw new ArgumentOutOfRangeException(nameof(planName))
        };

        // A fresh seller per plan so the Free-plan case never collides with another case's cap usage.
        var client = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(client, CategorySeedData.ElectronicsId, planId);

        var response = await PublishAsync(client, listing.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);

        published!.Status.Should().Be(ListingStatus.Active);
        published.PromotionPlanId.Should().Be(planId);
        published.PublishedAt.Should().NotBeNull();
        published.ExpiresAt.Should().NotBeNull();
        (published.ExpiresAt!.Value - published.PublishedAt!.Value).Should().Be(TimeSpan.FromDays(expectedDurationDays));
    }
}
