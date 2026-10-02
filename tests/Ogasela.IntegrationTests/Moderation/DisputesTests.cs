using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Admin;
using Ogasela.IntegrationTests.Listings;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Moderation;

public class DisputesTests : IClassFixture<ListingsApiFactory>
{
    private readonly ListingsApiFactory _factory;

    public DisputesTests(ListingsApiFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent RaiseForm(Guid listingId, Guid? conversationId = null, string reason = "NotDelivered") =>
        new()
        {
            { new StringContent(listingId.ToString()), "listingId" },
            { new StringContent(conversationId?.ToString() ?? string.Empty), "conversationId" },
            { new StringContent(reason), "reason" },
            { new StringContent("I paid for the phone a week ago and it never arrived."), "description" },
        };

    private static MultipartFormDataContent MessageForm(string body) => new() { { new StringContent(body), "body" } };

    private static async Task<IReadOnlyList<string>> InboxTypesAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/me/inbox?pageSize=50"))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("type").GetString()!).ToList();

    [Fact]
    public async Task BuyerRaises_PartiesReply_StaffResolvesWithTakeDownAndSuspension()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);
        var buyer = await RegisterSellerAsync(_factory);
        var start = await buyer.PostAsJsonAsync("/api/v1/conversations", new { listingId = listing.Id });
        start.EnsureSuccessStatusCode();
        var conversationId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await buyer.PostAsJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { content = "I've sent the money" }))
            .EnsureSuccessStatusCode();

        // The buyer's chat is linked automatically.
        var raise = await buyer.PostAsync("/api/v1/disputes", RaiseForm(listing.Id));
        raise.StatusCode.Should().Be(HttpStatusCode.OK, await raise.Content.ReadAsStringAsync());
        var summary = await raise.Content.ReadFromJsonAsync<JsonElement>();
        var disputeId = summary.GetProperty("id").GetGuid();
        var reference = summary.GetProperty("reference").GetString()!;
        reference.Should().StartWith("DSP-");
        summary.GetProperty("myRole").GetString().Should().Be("Raiser");

        (await buyer.PostAsync("/api/v1/disputes", RaiseForm(listing.Id))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await InboxTypesAsync(seller)).Should().Contain("DisputeOpened");

        var mine = await seller.GetFromJsonAsync<JsonElement>("/api/v1/disputes/mine");
        mine.EnumerateArray().Single(d => d.GetProperty("id").GetGuid() == disputeId).GetProperty("myRole").GetString().Should().Be("Respondent");
        (await seller.PostAsync($"/api/v1/disputes/{disputeId}/messages", MessageForm("I shipped it on Monday."))).EnsureSuccessStatusCode();
        (await InboxTypesAsync(buyer)).Should().Contain("DisputeUpdate");

        var outsider = await RegisterSellerAsync(_factory);
        (await outsider.GetAsync($"/api/v1/disputes/{disputeId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var (admin, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);
        var list = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/disputes?search={reference}");
        list.GetProperty("items").EnumerateArray().Single().GetProperty("messageCount").GetInt32().Should().Be(1);

        (await admin.PostAsync($"/api/v1/admin/disputes/{disputeId}/assign-to-me", null)).EnsureSuccessStatusCode();
        (await admin.PostAsJsonAsync($"/api/v1/admin/disputes/{disputeId}/messages", new { body = "Please share the waybill." }))
            .EnsureSuccessStatusCode();

        var detail = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/disputes/{disputeId}");
        detail.GetProperty("status").GetString().Should().Be("InReview");
        detail.GetProperty("chat").EnumerateArray().Single().GetProperty("role").GetString().Should().Be("Buyer");
        detail.GetProperty("messages").GetArrayLength().Should().Be(2);
        var sellerUserId = detail.GetProperty("against").GetProperty("userId").GetGuid();

        (await admin.PostAsJsonAsync($"/api/v1/admin/disputes/{disputeId}/resolve", new
        {
            outcome = "InFavourOfRaiser",
            note = "No proof of delivery was provided.",
            takeDownListing = true,
            suspendUserId = sellerUserId,
        })).EnsureSuccessStatusCode();

        var buyerView = await buyer.GetFromJsonAsync<JsonElement>($"/api/v1/disputes/{disputeId}");
        buyerView.GetProperty("summary").GetProperty("status").GetString().Should().Be("Resolved");
        buyerView.GetProperty("resolutionNote").GetString().Should().Be("No proof of delivery was provided.");
        (await InboxTypesAsync(buyer)).Should().Contain("DisputeResolved");

        var ad = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/listings/{listing.Id}");
        ad.GetProperty("status").GetString().Should().Be("Paused");
        var user = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/admin/users/{sellerUserId}");
        user.GetProperty("isSuspended").GetBoolean().Should().BeTrue();

        (await buyer.PostAsync($"/api/v1/disputes/{disputeId}/messages", MessageForm("Thanks")))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SellerMustNameTheChat_AndCanOnlySuspendAParty()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(seller, CategorySeedData.PhonesAndTabletsId);

        (await seller.PostAsync("/api/v1/disputes", RaiseForm(listing.Id, reason: "BuyerDidNotPay")))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var buyer = await RegisterSellerAsync(_factory);
        var start = await buyer.PostAsJsonAsync("/api/v1/conversations", new { listingId = listing.Id });
        var conversationId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var raise = await seller.PostAsync("/api/v1/disputes", RaiseForm(listing.Id, conversationId, "BuyerDidNotPay"));
        raise.EnsureSuccessStatusCode();
        var disputeId = (await raise.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await InboxTypesAsync(buyer)).Should().Contain("DisputeOpened");

        var (admin, adminId) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);
        (await admin.PostAsJsonAsync($"/api/v1/admin/disputes/{disputeId}/resolve", new
        {
            outcome = "Dismissed", note = "Not enough evidence.", takeDownListing = false, suspendUserId = adminId,
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await buyer.GetAsync("/api/v1/admin/disputes")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
