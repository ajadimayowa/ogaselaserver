using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Listings;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.IntegrationTests.Admin;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Listings;

public class ListingApprovalTests : IClassFixture<ApprovalListingsApiFactory>
{
    private readonly ApprovalListingsApiFactory _factory;

    public ListingApprovalTests(ApprovalListingsApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Seller, ListingResponse Listing)> PublishPendingAsync()
    {
        var seller = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateDraftListingAsync(seller, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.FreeId);
        var publish = await PublishAsync(seller, listing.Id);
        publish.EnsureSuccessStatusCode();
        return (seller, listing);
    }

    private static async Task<ListingDetailResponse?> GetDetailAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/v1/listings/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ListingDetailResponse>(JsonOptions)
            : null;
    }

    [Fact]
    public async Task Publish_PutsTheListingInPendingReview_HiddenFromEveryoneButTheSeller()
    {
        var (seller, listing) = await PublishPendingAsync();

        (await GetDetailAsync(seller, listing.Id))!.Status.Should().Be(ListingStatus.PendingReview);

        var anonymous = await _factory.CreateClient().GetAsync($"/api/v1/listings/{listing.Id}");
        anonymous.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ByModerator_MakesTheListingLiveWithAnExpiry()
    {
        var (seller, listing) = await PublishPendingAsync();
        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);

        var pending = await moderator.GetFromJsonAsync<List<PendingItem>>("/api/v1/admin/listings/pending", JsonOptions);
        pending!.Select(p => p.Id).Should().Contain(listing.Id);

        var approve = await moderator.PostAsync($"/api/v1/admin/listings/{listing.Id}/approve", null);
        approve.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await GetDetailAsync(_factory.CreateClient(), listing.Id);
        detail!.Status.Should().Be(ListingStatus.Active);
        detail.ExpiresAt.Should().NotBeNull();

        var again = await moderator.PostAsync($"/api/v1/admin/listings/{listing.Id}/approve", null);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reject_ByModerator_SendsTheListingBackToDraftWithTheReason()
    {
        var (seller, listing) = await PublishPendingAsync();
        var (moderator, _) = await AdminTestHelpers.RegisterInternalUserAsync(_factory, UserRole.Moderator);

        var reject = await moderator.PostAsJsonAsync(
            $"/api/v1/admin/listings/{listing.Id}/reject", new { reason = "Photos don't match the item." });
        reject.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await GetDetailAsync(seller, listing.Id);
        detail!.Status.Should().Be(ListingStatus.Draft);
        detail.ReviewNote.Should().Be("Photos don't match the item.");
    }

    [Fact]
    public async Task Approve_BySeller_IsForbidden()
    {
        var (seller, listing) = await PublishPendingAsync();

        var response = await seller.PostAsync($"/api/v1/admin/listings/{listing.Id}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record PendingItem(Guid Id, string Title);
}
