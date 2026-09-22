using FluentAssertions;
using Ogasela.Application.Verification;
using Ogasela.Application.Verification.ManualReviewDecision;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Verification;
using Ogasela.Infrastructure.Verification;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Verification;

public class ManualReviewDecisionCommandHandlerTests
{
    private static (ManualReviewDecisionCommandHandler Handler, TestApplicationDbContext DbContext, FakeCurrentUserService Moderator, FakeDateTime DateTime, SellerProfile Seller, BiometricVerification PendingVerification)
        CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();

        var user = User.Create("08031234567", null, "hash", UserRole.Seller);
        dbContext.Users.Add(user);
        var seller = SellerProfile.Create(user.Id, "Ada's Fabrics", null, null);
        dbContext.SellerProfiles.Add(seller);

        var dateTime = new FakeDateTime();
        var pending = BiometricVerification.Create(
            seller.Id, 1, "selfie-key", "id-photo-key", 85m, 85m,
            VerificationDecision.ManualReview, dateTime.UtcNow, 30);
        dbContext.BiometricVerifications.Add(pending);

        var moderatorUser = User.Create("08039999999", null, "hash", UserRole.Moderator);
        dbContext.Users.Add(moderatorUser);

        dbContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        var moderator = new FakeCurrentUserService { UserId = moderatorUser.Id };
        var faceProvider = new MockFaceVerificationProvider();
        var handler = new ManualReviewDecisionCommandHandler(dbContext, moderator, dateTime, faceProvider, new FakePublisher());

        return (handler, dbContext, moderator, dateTime, seller, pending);
    }

    [Fact]
    public async Task Handle_WhenModeratorApproves_VerifiesTheSellerAndEnrollsTheFace()
    {
        var (handler, dbContext, moderator, _, seller, pending) = CreateSut();

        var result = await handler.Handle(
            new ManualReviewDecisionCommand(pending.Id, VerificationDecision.Verified, "Looks good"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.Verified);

        var record = await dbContext.BiometricVerifications.FindAsync(pending.Id);
        record!.DecisionSource.Should().Be(VerificationDecisionSource.Manual);
        record.ReviewerId.Should().Be(moderator.UserId);
        record.RekognitionFaceId.Should().NotBeNullOrWhiteSpace();

        var reloadedSeller = await dbContext.SellerProfiles.FindAsync(seller.Id);
        reloadedSeller!.VerificationStatus.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Verified);

        dbContext.VerificationAuditEntries.Should().ContainSingle(a => a.BiometricVerificationId == pending.Id);
    }

    [Fact]
    public async Task Handle_WhenModeratorRejects_MarksTheSellerFailed()
    {
        var (handler, dbContext, _, _, seller, pending) = CreateSut();

        var result = await handler.Handle(
            new ManualReviewDecisionCommand(pending.Id, VerificationDecision.Failed, "ID photo mismatch"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.Failed);

        var reloadedSeller = await dbContext.SellerProfiles.FindAsync(seller.Id);
        reloadedSeller!.VerificationStatus.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenTheRecordIsNotAwaitingReview_ReturnsAnError()
    {
        var (handler, dbContext, _, dateTime, seller, _) = CreateSut();

        var alreadyDecided = BiometricVerification.Create(
            seller.Id, 1, "selfie-key-2", "id-photo-key-2", 95m, 95m,
            VerificationDecision.Verified, dateTime.UtcNow, 30);
        dbContext.BiometricVerifications.Add(alreadyDecided);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(
            new ManualReviewDecisionCommand(alreadyDecided.Id, VerificationDecision.Verified, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(VerificationErrors.NotPendingManualReview.Code);
    }
}
