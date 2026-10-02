using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class AnalyticsTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public AnalyticsTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BuyerEngagement_IsCountedForTheSeller_AndTheSellersOwnActivityIsNot()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var buyer = await RegisterSellerAsync(_factory);

        // Impression: appears in a search result page.
        var search = await buyer.GetAsync($"/api/v1/search?categoryId={CategorySeedData.PhonesAndTabletsId}&pageSize=100");
        search.EnsureSuccessStatusCode();

        // View, call tap, new chat, profile visit - by the buyer.
        (await buyer.GetAsync($"/api/v1/listings/{listing.Id}")).EnsureSuccessStatusCode();
        (await buyer.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/engagements", new { engagement = "CallClick" })).EnsureSuccessStatusCode();
        (await buyer.PostAsJsonAsync("/api/v1/conversations", new { listingId = listing.Id })).EnsureSuccessStatusCode();
        (await buyer.GetAsync($"/api/v1/sellers/{listing.SellerId}")).EnsureSuccessStatusCode();

        // The seller's own activity must not count.
        (await seller.GetAsync($"/api/v1/listings/{listing.Id}")).EnsureSuccessStatusCode();
        (await seller.GetAsync($"/api/v1/sellers/{listing.SellerId}")).EnsureSuccessStatusCode();

        var summary = await seller.GetFromJsonAsync<JsonElement>("/api/v1/me/analytics?days=7");
        summary.GetProperty("profileVisits").GetProperty("current").GetInt64().Should().Be(1);
        summary.GetProperty("impressions").GetProperty("current").GetInt64().Should().BeGreaterThanOrEqualTo(1);
        summary.GetProperty("views").GetProperty("current").GetInt64().Should().Be(1);
        summary.GetProperty("callClicks").GetProperty("current").GetInt64().Should().Be(1);
        summary.GetProperty("messageStarts").GetProperty("current").GetInt64().Should().Be(1);

        var perAd = await seller.GetFromJsonAsync<JsonElement>($"/api/v1/me/analytics/listings/{listing.Id}?days=7");
        perAd.GetProperty("ogasela").GetProperty("views").GetProperty("current").GetInt64().Should().Be(1);
        var platforms = perAd.GetProperty("platforms").EnumerateArray().ToList();
        platforms.Select(p => p.GetProperty("platform").GetString()).Should().BeEquivalentTo("Facebook", "TikTok");
        platforms.Should().OnlyContain(p => !p.GetProperty("promoted").GetBoolean());
    }

    [Fact]
    public async Task ListingAnalytics_ForSomeoneElsesAd_IsNotFound()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var otherSeller = await RegisterSellerAsync(_factory);

        var response = await otherSeller.GetAsync($"/api/v1/me/analytics/listings/{listing.Id}?days=30");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RecordEngagement_RejectsServerSideOnlyTypes()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var buyer = await RegisterSellerAsync(_factory);

        var response = await buyer.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/engagements", new { engagement = "View" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
