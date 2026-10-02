using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Listings;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Listings;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Ai;

/// <summary>
/// Uses the default ListingsApiFactory unmodified - HeuristicFraudRiskScorer needs no external
/// credentials (pure heuristics + Listing/SellerProfile data), so there's nothing to fake here.
/// </summary>
public class FraudRiskPublishTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public FraudRiskPublishTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Publish_OfAHighFraudRiskListing_StillPublishesButOpensAReportForModeration()
    {
        // A listing now needs a photo to be submitted, so the scorer's no-photos signal can't fire
        // here; the two that do (suspicious language 0.30 + brand-new account 0.15 = 0.45) are
        // checked against a threshold just below that.
        var factory = _factory.WithWebHostBuilder(b => b.UseSetting("Ai:FraudRiskThreshold", "0.4"));
        var sellerClient = await RegisterAndVerifySellerAsync(factory);

        // Deliberately trips two of HeuristicFraudRiskScorer's signals: suspicious
        // off-platform-payment language and a brand-new seller account (this test's own seller,
        // registered moments ago).
        var request = new CreateListingRequest(
            "URGENT SALE - cash only, no returns",
            "Selling fast, wire transfer preferred, no inspection allowed.",
            CategorySeedData.PhonesAndTabletsId,
            Price: 15000m,
            ListingCondition.Used,
            MediaUrls: ["https://example.com/photo1.jpg"],
            PromotionPlanSeedData.FreeId);

        var createResponse = await sellerClient.PostAsJsonAsync("/api/v1/listings", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listing = await createResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);

        var publishResponse = await PublishAsync(sellerClient, listing!.Id);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, "a high fraud-risk score must never block the publish");
        var published = await publishResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        published!.Status.Should().Be(ListingStatus.Active);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        var report = await dbContext.Reports.FirstOrDefaultAsync(r =>
            r.TargetType == ReportTargetType.Listing && r.TargetId == listing.Id);

        report.Should().NotBeNull("a high-risk publish must open a Report for Phase 11's moderation queue");
        report!.ReporterId.Should().BeNull("this report is system-generated, not filed by a human");
        report.Status.Should().Be(ReportStatus.Open);
    }
}
