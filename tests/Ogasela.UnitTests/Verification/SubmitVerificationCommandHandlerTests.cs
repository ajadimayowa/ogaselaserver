using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Ogasela.Application.Verification;
using Ogasela.Application.Verification.SubmitVerification;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Verification;
using Ogasela.Infrastructure.Verification;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Verification;

public class SubmitVerificationCommandHandlerTests
{
    private static (SubmitVerificationCommandHandler Handler, TestApplicationDbContext DbContext, FakeDateTime DateTime, SellerProfile Seller)
        CreateSut(VerificationSettings? settings = null)
    {
        var dbContext = TestApplicationDbContext.Create();

        var user = User.Create("08031234567", null, "hash", UserRole.Seller);
        dbContext.Users.Add(user);
        var seller = SellerProfile.Create(user.Id, "Ada's Fabrics", null, null);
        dbContext.SellerProfiles.Add(seller);
        dbContext.BiometricConsents.Add(BiometricConsent.Create(seller.Id, "1.0", DateTime.UtcNow, "127.0.0.1"));
        dbContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        var currentUser = new FakeCurrentUserService { UserId = user.Id };
        var dateTime = new FakeDateTime();
        var faceProvider = new MockFaceVerificationProvider();
        var imageStorage = new InMemoryBiometricImageStorage();
        var options = Options.Create(settings ?? new VerificationSettings());

        var handler = new SubmitVerificationCommandHandler(
            dbContext, currentUser, dateTime, faceProvider, imageStorage, new FakePublisher(), options);

        return (handler, dbContext, dateTime, seller);
    }

    private static SubmitVerificationCommand BuildCommand(
        string selfieFileName = "selfie.jpg", string idPhotoFileName = "id.jpg", string livenessSessionRef = "liveness-ok-session")
    {
        return new SubmitVerificationCommand(
            new MemoryStream(Encoding.UTF8.GetBytes("selfie-bytes")), selfieFileName, "image/jpeg",
            new MemoryStream(Encoding.UTF8.GetBytes("id-photo-bytes")), idPhotoFileName, "image/jpeg",
            livenessSessionRef);
    }

    [Fact]
    public async Task Handle_WithHighSimilarityAndPassingLiveness_ReturnsVerifiedAndEnrollsFace()
    {
        var (handler, dbContext, _, seller) = CreateSut();

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.Verified);

        var reloadedSeller = await dbContext.SellerProfiles.FindAsync(seller.Id);
        reloadedSeller!.VerificationStatus.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Verified);

        var record = await dbContext.BiometricVerifications.FindAsync(result.Value.VerificationId);
        record!.RekognitionFaceId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WithMidRangeSimilarity_ReturnsManualReview()
    {
        var (handler, dbContext, _, seller) = CreateSut();

        var result = await handler.Handle(BuildCommand(selfieFileName: "selfie-score85.jpg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.ManualReview);

        var reloadedSeller = await dbContext.SellerProfiles.FindAsync(seller.Id);
        reloadedSeller!.VerificationStatus.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.ManualReview);
    }

    [Fact]
    public async Task Handle_WithLowSimilarity_ReturnsFailed()
    {
        var (handler, dbContext, _, seller) = CreateSut();

        var result = await handler.Handle(BuildCommand(selfieFileName: "selfie-score60.jpg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.Failed);

        var reloadedSeller = await dbContext.SellerProfiles.FindAsync(seller.Id);
        reloadedSeller!.VerificationStatus.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Failed);
    }

    [Fact]
    public async Task Handle_WithFailingLiveness_ReturnsFailedEvenWithHighSimilarity()
    {
        var (handler, _, _, _) = CreateSut();

        var result = await handler.Handle(
            BuildCommand(livenessSessionRef: "liveness-fail-session"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.Failed);
    }

    [Fact]
    public async Task Handle_WhenAnExistingEnrollmentMatches_OverridesVerifiedToManualReview()
    {
        var (handler, dbContext, _, seller) = CreateSut();

        var result = await handler.Handle(
            BuildCommand(selfieFileName: "selfie-duplicate.jpg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be(VerificationDecision.ManualReview);

        var record = await dbContext.BiometricVerifications.FindAsync(result.Value.VerificationId);
        record!.RekognitionFaceId.Should().BeNull("a duplicate match must not be enrolled");
    }

    [Fact]
    public async Task Handle_OnTheThirdFailingAttempt_ForcesManualReviewInsteadOfAnotherFailure()
    {
        var (handler, dbContext, dateTime, seller) = CreateSut();

        // Seed two prior failed attempts so the next submission is the third.
        for (var i = 1; i <= 2; i++)
        {
            var priorAttempt = BiometricVerification.Create(
                seller.Id, i, "prior-selfie-key", "prior-id-key", 30m, 60m,
                VerificationDecision.Failed, dateTime.UtcNow, 30);
            dbContext.BiometricVerifications.Add(priorAttempt);
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(BuildCommand(selfieFileName: "selfie-score60.jpg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AttemptNumber.Should().Be(3);
        result.Value.Decision.Should().Be(VerificationDecision.ManualReview);
    }
}
