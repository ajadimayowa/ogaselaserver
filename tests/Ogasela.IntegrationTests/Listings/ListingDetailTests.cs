using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class ListingDetailTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public ListingDetailTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ForSignedInCaller_IncludesSellerSummaryCategoryPlanAndPhone()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);
        var buyerClient = await RegisterSellerAsync(_factory);

        var response = await buyerClient.GetAsync($"/api/v1/listings/{listing.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<ListingDetailResponse>(JsonOptions);
        detail!.CategoryName.Should().Be("Phones & Tablets");
        detail.PromotionPlanName.Should().Be("Free");
        detail.Seller.SellerProfileId.Should().Be(listing.SellerId);
        detail.Seller.BusinessName.Should().NotBeNullOrWhiteSpace();
        detail.Seller.Phone.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_ForAnonymousCaller_HidesSellerPhone()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);

        var response = await _factory.CreateClient().GetAsync($"/api/v1/listings/{listing.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<ListingDetailResponse>(JsonOptions);
        detail!.Seller.Phone.Should().BeNull();
        detail.Seller.BusinessName.Should().NotBeNullOrWhiteSpace();
    }
}
