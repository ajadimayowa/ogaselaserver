using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Admin;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class InboxTests : IClassFixture<ApprovalListingsApiFactory>
{
    private readonly ApprovalListingsApiFactory _factory;

    public InboxTests(ApprovalListingsApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> InboxAsync(HttpClient client) =>
        await client.GetFromJsonAsync<JsonElement>("/api/v1/me/inbox?pageSize=50");

    [Fact]
    public async Task Approval_AndMessages_ShowUpInTheRecipientsInbox_GroupedPerChat()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(seller, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.FreeId);
        (await PublishAsync(seller, listing.Id)).EnsureSuccessStatusCode();
        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);
        (await moderator.PostAsync($"/api/v1/admin/listings/{listing.Id}/approve", null)).EnsureSuccessStatusCode();

        var buyer = await RegisterSellerAsync(_factory);
        var start = await buyer.PostAsJsonAsync("/api/v1/conversations", new { listingId = listing.Id });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await buyer.PostAsJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { content = "Is it still available?" })).EnsureSuccessStatusCode();
        (await buyer.PostAsJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { content = "Last price?" })).EnsureSuccessStatusCode();

        var inbox = await InboxAsync(seller);
        var items = inbox.GetProperty("items").EnumerateArray().ToList();
        // Verifying the test seller also (correctly) leaves a "You're verified" entry.
        items.Select(i => i.GetProperty("type").GetString()).Should().BeEquivalentTo("Verification", "AdApproved", "NewMessage");
        var message = items.Single(i => i.GetProperty("type").GetString() == "NewMessage");
        message.GetProperty("count").GetInt32().Should().Be(2);
        message.GetProperty("body").GetString().Should().Be("Last price?");
        inbox.GetProperty("unreadCount").GetInt32().Should().Be(3);
        (await seller.GetFromJsonAsync<int>("/api/v1/me/inbox/unread-count")).Should().Be(3);

        (await seller.PostAsync($"/api/v1/me/inbox/{message.GetProperty("id").GetGuid()}/read", null)).EnsureSuccessStatusCode();
        (await seller.GetFromJsonAsync<int>("/api/v1/me/inbox/unread-count")).Should().Be(2);

        (await seller.PostAsync("/api/v1/me/inbox/read-all", null)).EnsureSuccessStatusCode();
        (await seller.GetFromJsonAsync<int>("/api/v1/me/inbox/unread-count")).Should().Be(0);
    }

    [Fact]
    public async Task Rejection_NotifiesTheSellerWithTheReason()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(seller, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.FreeId);
        (await PublishAsync(seller, listing.Id)).EnsureSuccessStatusCode();
        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);

        (await moderator.PostAsJsonAsync($"/api/v1/admin/listings/{listing.Id}/reject", new { reason = "Blurry photos" })).EnsureSuccessStatusCode();

        var item = (await InboxAsync(seller)).GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("type").GetString() == "AdRejected");
        item.GetProperty("body").GetString().Should().Contain("Blurry photos");
        item.GetProperty("listingId").GetGuid().Should().Be(listing.Id);
    }
}
