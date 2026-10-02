using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Payments;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Listings;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Admin;

public class AdminRevenueTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public AdminRevenueTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuperAdmin_SeesEarnedRevenueSeparateFromTopUps_AndTheLedger()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var me = await seller.GetFromJsonAsync<JsonElement>("/api/v1/me");
        var userId = me.GetProperty("id").GetGuid();

        string reference;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
            var sellerId = (await db.SellerProfiles.FirstAsync(s => s.UserId == userId)).Id;
            var now = DateTime.UtcNow;
            reference = $"REV-{Guid.NewGuid():N}"[..16];
            db.Transactions.Add(Transaction.CreateSucceeded(sellerId, TransactionType.AdSpendTopUp, 500_000m, reference, null, now));
            db.Transactions.Add(Transaction.CreateSucceeded(sellerId, TransactionType.PlanPurchase, 250_000m, null, listing.Id, now));
            db.Transactions.Add(Transaction.CreatePending(sellerId, TransactionType.AdSpendTopUp, 100_000m, reference + "P", now));
            await db.SaveChangesAsync();
        }

        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);
        var summary = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/revenue/summary");
        summary.GetProperty("revenueKobo").GetDecimal().Should().BeGreaterThanOrEqualTo(250_000m);
        summary.GetProperty("topUpsKobo").GetDecimal().Should().BeGreaterThanOrEqualTo(500_000m);
        summary.GetProperty("pendingTopUps").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        summary.GetProperty("bucket").GetString().Should().Be("day");
        summary.GetProperty("series").GetArrayLength().Should().Be(30);
        summary.GetProperty("topSellers").EnumerateArray().Should().Contain(s => s.GetProperty("userId").GetGuid() == userId);

        var ledger = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/revenue/transactions?search={reference}");
        ledger.GetProperty("items").GetArrayLength().Should().Be(2);
        var plans = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/revenue/transactions?type=PlanPurchase&pageSize=100");
        var purchase = plans.GetProperty("items").EnumerateArray().First(i => i.GetProperty("listingId").GetGuid() == listing.Id);
        purchase.GetProperty("planName").GetString().Should().Be("Free");
        purchase.GetProperty("listingTitle").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task OtherStaff_CannotSeeRevenue()
    {
        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);

        (await moderator.GetAsync("/api/v1/admin/revenue/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
