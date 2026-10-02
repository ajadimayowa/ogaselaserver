using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Domain.Moderation;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class ReportListingTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public ReportListingTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Report_ByAnotherUser_OpensOneListingReportEvenIfSentTwice()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);
        var reporterClient = await RegisterSellerAsync(_factory);

        var first = await reporterClient.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/report", new { reason = "Looks like a scam" });
        var second = await reporterClient.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/report", new { reason = "Still a scam" });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        var reports = await dbContext.Reports
            .Where(r => r.TargetType == ReportTargetType.Listing && r.TargetId == listing.Id && r.ReporterId != null)
            .ToListAsync();
        reports.Should().ContainSingle().Which.Status.Should().Be(ReportStatus.Open);
    }

    [Fact]
    public async Task Report_OwnListing_IsRejected()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);

        var response = await sellerClient.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/report", new { reason = "Testing" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Title.Should().Be("Moderation.CannotReportOwnListing");
    }

    [Fact]
    public async Task Report_WhenSignedOut_IsUnauthorized()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);

        var response = await _factory.CreateClient().PostAsJsonAsync($"/api/v1/listings/{listing.Id}/report", new { reason = "Spam" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ProblemDetailsResponse(string Title, string Detail);
}
