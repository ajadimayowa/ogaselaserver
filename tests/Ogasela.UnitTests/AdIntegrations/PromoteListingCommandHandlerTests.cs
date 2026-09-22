using FluentAssertions;
using Ogasela.Application.Ai;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Promotions;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.AdIntegrations;

/// <summary>
/// Covers the PromoteListing plan gate: PromotionPlan.AdPlatformPushAllowed, which is false only
/// for the Free plan (every paid plan - Basic, Standard, Premium - has it set true). AdCopyTitle
/// and AdCopyDescription are always supplied directly here so the AI-assist soft-fallback path
/// (a separate, non-blocking concern) never runs and doesn't confound this gate's assertions.
/// </summary>
public class PromoteListingCommandHandlerTests
{
    private sealed record Sut(
        PromoteListingCommandHandler Handler, TestApplicationDbContext DbContext, Guid SellerId, Guid FreeListingId, Guid PaidListingId);

    private static Sut CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();

        var freePlan = PromotionPlan.Create(
            Guid.NewGuid(), PromotionPlanName.Free, 7, 3, false, 0, 0m, AiToolTier.Basic, adPlatformPushAllowed: false, 0, true);
        var standardPlan = PromotionPlan.Create(
            Guid.NewGuid(), PromotionPlanName.Standard, 21, 10, true, 2, 200000m, AiToolTier.Standard, adPlatformPushAllowed: true, 0, true);

        var user = User.Create("08011112222", null, "hashed-password", UserRole.Seller);
        var sellerProfile = SellerProfile.Create(user.Id, "Ada's Fabrics", null, null);

        var freeListing = Listing.CreateDraft(
            Guid.NewGuid(), sellerProfile.Id, Guid.NewGuid(), freePlan.Id, "A free-plan item", "Description",
            5000m, ListingCondition.Used, ["https://example.com/photo.jpg"], DateTime.UtcNow);
        var paidListing = Listing.CreateDraft(
            Guid.NewGuid(), sellerProfile.Id, Guid.NewGuid(), standardPlan.Id, "A standard-plan item", "Description",
            5000m, ListingCondition.Used, ["https://example.com/photo.jpg"], DateTime.UtcNow);

        var connection = AdAccountConnection.Create(
            Guid.NewGuid(), sellerProfile.Id, AdPlatform.Facebook, "encrypted-token", null, "external-account", DateTime.UtcNow);

        dbContext.PromotionPlans.AddRange(freePlan, standardPlan);
        dbContext.Users.Add(user);
        dbContext.SellerProfiles.Add(sellerProfile);
        dbContext.Listings.AddRange(freeListing, paidListing);
        dbContext.AdAccountConnections.Add(connection);
        dbContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        var currentUser = new FakeCurrentUserService { UserId = user.Id };
        var clientFactory = new FakeAdPlatformClientFactory(new FakeAdPlatformClient(AdPlatform.Facebook));

        var handler = new PromoteListingCommandHandler(
            dbContext, currentUser, new FakeDateTime(), clientFactory, new FakeTokenEncryptor(),
            new AiAccessGuard(dbContext), new FakeListingCopyGenerator());

        return new Sut(handler, dbContext, sellerProfile.Id, freeListing.Id, paidListing.Id);
    }

    [Fact]
    public async Task Handle_ForAFreePlanListing_RejectsPromotion()
    {
        var sut = CreateSut();

        var command = new PromoteListingCommand(
            sut.FreeListingId, AdPlatform.Facebook, 500000m, 7, null, "My ad title", "My ad description");

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdIntegrationErrors.PromotionNotAllowed.Code);
    }

    [Fact]
    public async Task Handle_ForAPaidPlanListing_AllowsPromotion()
    {
        var sut = CreateSut();

        var command = new PromoteListingCommand(
            sut.PaidListingId, AdPlatform.Facebook, 500000m, 7, null, "My ad title", "My ad description");

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AdCampaignStatus.PendingReview);
    }

    [Fact]
    public async Task Handle_WithNoConnectedAdAccount_ReturnsConnectionNotFound()
    {
        var sut = CreateSut();

        // Promote a paid listing on a platform the seller never connected.
        var command = new PromoteListingCommand(
            sut.PaidListingId, AdPlatform.TikTok, 500000m, 7, null, "My ad title", "My ad description");

        var result = await sut.Handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdIntegrationErrors.ConnectionNotFound.Code);
    }
}
