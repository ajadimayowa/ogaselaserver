using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Verification.Events;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Domain.Verification;
using Ogasela.Shared;
using AccountVerificationStatus = Ogasela.Domain.Accounts.VerificationStatus;

namespace Ogasela.Application.Verification.ManualReviewDecision;

public sealed class ManualReviewDecisionCommandHandler
    : IRequestHandler<ManualReviewDecisionCommand, Result<ManualReviewDecisionResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IFaceVerificationProvider _faceProvider;
    private readonly IPublisher _publisher;

    public ManualReviewDecisionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTime dateTime,
        IFaceVerificationProvider faceProvider,
        IPublisher publisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _faceProvider = faceProvider;
        _publisher = publisher;
    }

    public async Task<Result<ManualReviewDecisionResponse>> Handle(
        ManualReviewDecisionCommand request, CancellationToken cancellationToken)
    {
        if (request.Decision is not (VerificationDecision.Verified or VerificationDecision.Failed))
        {
            return Result.Failure<ManualReviewDecisionResponse>(VerificationErrors.InvalidManualDecision);
        }

        var verification = await _dbContext.BiometricVerifications
            .FirstOrDefaultAsync(v => v.Id == request.VerificationId, cancellationToken);

        if (verification is null)
        {
            return Result.Failure<ManualReviewDecisionResponse>(VerificationErrors.VerificationNotFound);
        }

        if (verification.Decision != VerificationDecision.ManualReview)
        {
            return Result.Failure<ManualReviewDecisionResponse>(VerificationErrors.NotPendingManualReview);
        }

        if (request.Decision == VerificationDecision.Verified)
        {
            if (!verification.HasRawImages)
            {
                return Result.Failure<ManualReviewDecisionResponse>(VerificationErrors.RawImagesUnavailable);
            }

            var rekognitionFaceId = await _faceProvider.EnrollFaceAsync(
                verification.SelfieImageS3Key!, verification.SellerId, cancellationToken);
            verification.Enroll(rekognitionFaceId);
        }

        var reviewerId = _currentUser.UserId!.Value;
        var now = _dateTime.UtcNow;

        verification.ApplyManualReview(request.Decision, reviewerId, now);

        var sellerProfile = await _dbContext.SellerProfiles
            .FirstOrDefaultAsync(s => s.Id == verification.SellerId, cancellationToken);

        sellerProfile?.UpdateVerificationStatus(
            request.Decision == VerificationDecision.Verified
                ? AccountVerificationStatus.Verified
                : AccountVerificationStatus.Failed,
            now);

        _dbContext.VerificationAuditEntries.Add(
            VerificationAuditEntry.Create(verification.Id, reviewerId, request.Decision, request.Notes, now));

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new VerificationDecisionEvent(verification.SellerId, request.Decision, now), cancellationToken);

        return Result.Success(new ManualReviewDecisionResponse(verification.Id, request.Decision, now));
    }
}
