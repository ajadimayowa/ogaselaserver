using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Listings;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Admin;

public class AdminSettingsTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public AdminSettingsTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    private static object PlanBody(string name, decimal priceKobo, params string[] features) => new
    {
        name,
        description = "For sellers who want more eyes on their ad.",
        features,
        durationDays = 14,
        photoLimit = 9,
        videoAllowed = true,
        boostWeight = 2,
        price = priceKobo,
        aiToolTier = "Standard",
        adPlatformPushAllowed = true,
        bundledAdCreditKobo = 0,
        isActive = true,
    };

    [Fact]
    public async Task SuperAdmin_AddsAndUpdatesPlans_WithWhatTheyOffer()
    {
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);
        var name = $"Boost {Guid.NewGuid():N}"[..14];

        var create = await admin.PostAsJsonAsync("/api/v1/admin/promotion-plans", PlanBody(name, 120_000m, "Live for 14 days", " ", "Video allowed"));
        create.StatusCode.Should().Be(HttpStatusCode.OK, await create.Content.ReadAsStringAsync());
        var plan = await create.Content.ReadFromJsonAsync<JsonElement>();
        var planId = plan.GetProperty("id").GetGuid();
        plan.GetProperty("features").EnumerateArray().Select(f => f.GetString()).Should().Equal("Live for 14 days", "Video allowed");

        (await admin.PostAsJsonAsync("/api/v1/admin/promotion-plans", PlanBody(name.ToUpperInvariant(), 1m)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await admin.PostAsJsonAsync("/api/v1/admin/promotion-plans", PlanBody($"Zero {Guid.NewGuid():N}"[..12], 0m)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "there is already a free plan");

        var renamed = name + " Plus";
        (await admin.PutAsJsonAsync($"/api/v1/admin/promotion-plans/{planId}", PlanBody(renamed, 150_000m, "Top of search")))
            .EnsureSuccessStatusCode();

        var plans = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/v1/promotion-plans");
        var updated = plans.EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == planId);
        updated.GetProperty("name").GetString().Should().Be(renamed);
        updated.GetProperty("price").GetDecimal().Should().Be(150_000m);
        updated.GetProperty("features").EnumerateArray().Single().GetString().Should().Be("Top of search");
        plans.EnumerateArray().Select(p => p.GetProperty("price").GetDecimal()).Should().BeInAscendingOrder();

        // The renamed free plan still behaves as the free plan - "free" is the price, not the name.
        var free = plans.EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == PromotionPlanSeedData.FreeId);
        free.GetProperty("features").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApprovalSwitch_DecidesWhetherNewAdsWaitForReview()
    {
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);
        var seller = await RegisterAndVerifySellerAsync(_factory);

        try
        {
            var on = await admin.PatchAsJsonAsync("/api/v1/admin/settings", new { requireListingApproval = true });
            on.EnsureSuccessStatusCode();
            (await on.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requireListingApproval").GetBoolean().Should().BeTrue();

            var pending = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
            pending.Status.ToString().Should().Be("PendingReview");

            (await admin.PatchAsJsonAsync("/api/v1/admin/settings", new { requireListingApproval = false })).EnsureSuccessStatusCode();
            var live = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
            live.Status.ToString().Should().Be("Active");
        }
        finally
        {
            (await admin.PatchAsJsonAsync("/api/v1/admin/settings", new { requireListingApproval = false })).EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task NotificationSwitches_AreSavedPerCategoryAndChannel_AndOnlySuperAdminsSeeSettings()
    {
        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);

        var response = await admin.PatchAsJsonAsync("/api/v1/admin/settings", new
        {
            notifications = new[] { new { category = "Verification", channel = "Email", enabled = false } },
        });
        response.EnsureSuccessStatusCode();
        var settings = await response.Content.ReadFromJsonAsync<JsonElement>();
        var verification = settings.GetProperty("notifications").EnumerateArray().Single(c => c.GetProperty("category").GetString() == "Verification");
        verification.GetProperty("channels").EnumerateArray()
            .Single(c => c.GetProperty("channel").GetString() == "Email").GetProperty("enabled").GetBoolean().Should().BeFalse();
        verification.GetProperty("channels").EnumerateArray()
            .Single(c => c.GetProperty("channel").GetString() == "Push").GetProperty("enabled").GetBoolean().Should().BeTrue();

        (await admin.PatchAsJsonAsync("/api/v1/admin/settings", new
        {
            notifications = new[] { new { category = "Nope", channel = "Email", enabled = false } },
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);
        (await moderator.GetAsync("/api/v1/admin/settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
