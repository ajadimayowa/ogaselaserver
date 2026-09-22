using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Verification.Events;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Domain.Verification;
using Ogasela.Shared;
using AccountVerificationStatus = Ogasela.Domain.Accounts.VerificationStatus;

namespace Ogasela.Application.Verification.SubmitVerification;

public sealed class SubmitVerificationCommandHandler
    : IRequestHandler<SubmitVerificationCommand, Result<SubmitVerificationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IFaceVerificationProvider _faceProvider;
    private readonly IBiometricImageStorage _imageStorage;
    private readonly IPublisher _publisher;
    private readonly VerificationSettings _settings;

    public SubmitVerificationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTime dateTime,
        IFaceVerificationProvider faceProvider,
        IBiometricImageStorage imageStorage,
        IPublisher publisher,
        IOptions<VerificationSettings> settings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _faceProvider = faceProvider;
        _imageStorage = imageStorage;
        _publisher = publisher;
        _settings = settings.Value;
    }

    public async Task<Result<SubmitVerificationResponse>> Handle(
        SubmitVerificationCommand request, CancellationToken cancellationToken)
    {
        var sellerProfile = await _dbContext.SellerProfiles
            .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId!.Value, cancellationToken);

        if (sellerProfile is null)
        {
            return Result.Failure<SubmitVerificationResponse>(VerificationErrors.SellerProfileNotFound);
        }

        if (sellerProfile.VerificationStatus == AccountVerificationStatus.Verified)
        {
            return Result.Failure<SubmitVerificationResponse>(VerificationErrors.AlreadyVerified);
        }

        var priorAttempts = await _dbContext.BiometricVerifications
            .Where(v => v.SellerId == sellerProfile.Id)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        if (priorAttempts.Any(v => v.Decision == VerificationDecision.ManualReview))
        {
            return Result.Failure<SubmitVerificationResponse>(VerificationErrors.PendingManualReview);
        }

        var hasConsent = await _dbContext.BiometricConsents
            .AnyAsync(c => c.SellerId == sellerProfile.Id, cancellationToken);

        if (!hasConsent)
        {
            return Result.Failure<SubmitVerificationResponse>(VerificationErrors.ConsentRequired);
        }

        var attemptNumber = priorAttempts.Count + 1;
        var now = _dateTime.UtcNow;

        var selfieKey = await _imageStorage.UploadAsync(
            sellerProfile.Id, request.SelfieFileName, request.SelfieContentType, request.SelfieImage, cancellationToken);
        var idPhotoKey = await _imageStorage.UploadAsync(
            sellerProfile.Id, request.IdPhotoFileName, request.IdPhotoContentType, request.IdPhotoImage, cancellationToken);

        decimal? livenessScore = null;
        decimal? idMatchScore = null;
        VerificationDecision decision;

        var quality = await _faceProvider.CheckQualityAsync(selfieKey, cancellationToken);
        if (!quality.Passed)
        {
            decision = VerificationDecision.Failed;
        }
        else
        {
            var liveness = await _faceProvider.CheckLivenessAsync(request.LivenessSessionRef, cancellationToken);
            var match = await _faceProvider.CompareFacesAsync(selfieKey, idPhotoKey, cancellationToken);

            livenessScore = liveness.Score;
            idMatchScore = match.SimilarityScore;

            // Liveness failure always fails the attempt outright - a high ID-match score on a
            // failed liveness check is exactly the spoofing scenario liveness detection exists
            // to catch, so it must never be softened into a manual-review outcome.
            decision = !liveness.Passed
                ? VerificationDecision.Failed
                : match.SimilarityScore >= _settings.VerifiedSimilarityThreshold
                    ? VerificationDecision.Verified
                    : match.SimilarityScore >= _settings.ManualReviewSimilarityThreshold
                        ? VerificationDecision.ManualReview
                        : VerificationDecision.Failed;
        }

        // A seller who has exhausted every automatic attempt is routed to a human instead of
        // being told to keep retrying indefinitely.
        if (decision == VerificationDecision.Failed && attemptNumber >= _settings.MaxAttempts)
        {
            decision = VerificationDecision.ManualReview;
        }

        if (decision == VerificationDecision.Verified)
        {
            var duplicateSellerId = await _faceProvider.FindDuplicateAsync(selfieKey, cancellationToken);
            if (duplicateSellerId is not null && duplicateSellerId != sellerProfile.Id)
            {
                decision = VerificationDecision.ManualReview;
            }
        }

        var verification = BiometricVerification.Create(
            sellerProfile.Id, attemptNumber, selfieKey, idPhotoKey, livenessScore, idMatchScore, decision, now,
            _settings.RawImageRetentionDays);

        if (decision == VerificationDecision.Verified)
        {
            var rekognitionFaceId = await _faceProvider.EnrollFaceAsync(selfieKey, sellerProfile.Id, cancellationToken);
            verification.Enroll(rekognitionFaceId);
        }

        _dbContext.BiometricVerifications.Add(verification);

        sellerProfile.UpdateVerificationStatus(ToAccountStatus(decision), now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new VerificationDecisionEvent(sellerProfile.Id, decision, now), cancellationToken);

        return Result.Success(new SubmitVerificationResponse(
            verification.Id, decision, attemptNumber, _settings.MaxAttempts));
    }

    private static AccountVerificationStatus ToAccountStatus(VerificationDecision decision) => decision switch
    {
        VerificationDecision.Verified => AccountVerificationStatus.Verified,
        VerificationDecision.ManualReview => AccountVerificationStatus.ManualReview,
        VerificationDecision.Failed => AccountVerificationStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
    };
}
