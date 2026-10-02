using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Admin;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class AdminUsersTests : IClassFixture<ListingsApiFactory>
{
    private const string SellerPassword = "Sup3rSecret!";
    private readonly ListingsApiFactory _factory;

    public AdminUsersTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Seller, Guid UserId, string Phone, Guid ListingId)> LiveSellerAsync()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var me = await seller.GetFromJsonAsync<JsonElement>("/api/v1/me");
        return (seller, me.GetProperty("id").GetGuid(), me.GetProperty("phone").GetString()!, listing.Id);
    }

    [Fact]
    public async Task SuperAdmin_CanListAndOpenUsers_SellersCannot()
    {
        var (seller, userId, phone, _) = await LiveSellerAsync();
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);

        var page = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users?search={phone}");
        var item = page.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("id").GetGuid().Should().Be(userId);
        item.GetProperty("liveAdCount").GetInt32().Should().Be(1);

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users/{userId}");
        detail.GetProperty("stats").GetProperty("liveAds").GetInt32().Should().Be(1);
        detail.GetProperty("seller").GetProperty("verificationStatus").GetString().Should().Be("Verified");

        (await seller.GetAsync("/api/v1/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Suspend_BlocksSignInAndPausesAds_ReactivateRestoresSignIn()
    {
        var (_, userId, phone, listingId) = await LiveSellerAsync();
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);

        (await admin.PostAsJsonAsync($"/api/v1/admin/users/{userId}/suspend", new { reason = "Reported for fraud" }))
            .EnsureSuccessStatusCode();

        var listing = await _factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/listings/{listingId}");
        listing.GetProperty("status").GetString().Should().Be("Paused");

        var blocked = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { phone, password = SellerPassword });
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users/{userId}");
        detail.GetProperty("isSuspended").GetBoolean().Should().BeTrue();
        detail.GetProperty("suspensionReason").GetString().Should().Be("Reported for fraud");
        detail.GetProperty("stats").GetProperty("activeSessions").GetInt32().Should().Be(0);

        (await admin.PostAsync($"/api/v1/admin/users/{userId}/reactivate", null)).EnsureSuccessStatusCode();
        var allowed = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { phone, password = SellerPassword });
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
