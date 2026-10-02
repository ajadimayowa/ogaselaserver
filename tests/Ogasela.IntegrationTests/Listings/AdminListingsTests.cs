using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Admin;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class AdminListingsTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public AdminListingsTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchByTitleOrSeller_TakeDown_Reinstate_ChangeCategory()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);

        // The helper's ads are titled "A great item"; its sellers are "Ada's Fabrics".
        var byTitle = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/listings?search=great%20item&pageSize=100");
        byTitle.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Contain(listing.Id);
        var bySeller = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/listings?search=ada%27s%20fabrics&pageSize=100");
        bySeller.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()).Should().Contain(listing.Id);

        (await admin.PostAsJsonAsync($"/api/v1/admin/listings/{listing.Id}/take-down", new { reason = "Counterfeit item" }))
            .EnsureSuccessStatusCode();
        var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/listings/{listing.Id}");
        detail.GetProperty("status").GetString().Should().Be("Paused");
        detail.GetProperty("reviewNote").GetString().Should().Be("Counterfeit item");
        detail.GetProperty("history").EnumerateArray().First().GetProperty("action").GetString().Should().Be("Listing.TakenDown");

        var inbox = await seller.GetFromJsonAsync<JsonElement>("/api/v1/me/inbox?pageSize=50");
        inbox.GetProperty("items").EnumerateArray().Should().Contain(i => i.GetProperty("type").GetString() == "AdTakenDown");

        (await admin.PostAsync($"/api/v1/admin/listings/{listing.Id}/reinstate", null)).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/v1/admin/listings/{listing.Id}/category", new { categoryId = CategorySeedData.ElectronicsId }))
            .EnsureSuccessStatusCode();

        detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/listings/{listing.Id}");
        detail.GetProperty("status").GetString().Should().Be("Active");
        detail.GetProperty("categoryName").GetString().Should().Be("Electronics");

        (await admin.PostAsync($"/api/v1/admin/listings/{listing.Id}/reinstate", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sellers_CannotUseTheAdminAdsEndpoints()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);

        (await seller.GetAsync("/api/v1/admin/listings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
