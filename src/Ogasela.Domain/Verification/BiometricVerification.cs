namespace Ogasela.Domain.Verification;

/// <summary>
/// One attempt by a seller to complete biometric identity verification. A new row is created
/// per submission (never overwritten in place) so the total-attempts count and audit trail stay
/// intact; only the raw image keys are nulled out once they expire or are erased.
/// </summary>
public class BiometricVerification
{
    private BiometricVerification()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public int AttemptNumber { get; private set; }

    public string? RekognitionFaceId { get; private set; }

    public decimal? LivenessScore { get; private set; }

    public decimal? IdMatchScore { get; private set; }

    public VerificationDecision Decision { get; private set; }

    public VerificationDecisionSource DecisionSource { get; private set; }

    public Guid? ReviewerId { get; private set; }

    /// <summary>S3 key of the raw selfie image. Nulled out once erased or past its retention window.</summary>
    public string? SelfieImageS3Key { get; private set; }

    /// <summary>S3 key of the raw ID photo image. Nulled out once erased or past its retention window.</summary>
    public string? IdPhotoImageS3Key { get; private set; }

    public DateTime? RawImageExpiryDate { get; private set; }

    public bool IsErased { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public static BiometricVerification Create(
        Guid sellerId,
        int attemptNumber,
        string selfieImageS3Key,
        string idPhotoImageS3Key,
        decimal? livenessScore,
        decimal? idMatchScore,
        VerificationDecision decision,
        DateTime createdAt,
        int rawImageRetentionDays)
    {
        return new BiometricVerification
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            AttemptNumber = attemptNumber,
            LivenessScore = livenessScore,
            IdMatchScore = idMatchScore,
            Decision = decision,
            DecisionSource = VerificationDecisionSource.Automatic,
            SelfieImageS3Key = selfieImageS3Key,
            IdPhotoImageS3Key = idPhotoImageS3Key,
            RawImageExpiryDate = createdAt.AddDays(rawImageRetentionDays),
            CreatedAt = createdAt,
            DecidedAt = createdAt
        };
    }

    public void Enroll(string rekognitionFaceId)
    {
        RekognitionFaceId = rekognitionFaceId;
    }

    public void ApplyManualReview(VerificationDecision decision, Guid reviewerId, DateTime decidedAt)
    {
        Decision = decision;
        DecisionSource = VerificationDecisionSource.Manual;
        ReviewerId = reviewerId;
        DecidedAt = decidedAt;
    }

    /// <summary>Called by the daily retention job once <see cref="RawImageExpiryDate"/> has passed.</summary>
    public void ExpireRawImages()
    {
        SelfieImageS3Key = null;
        IdPhotoImageS3Key = null;
    }

    /// <summary>Called for an NDPA data-erasure request; also clears any scores tied to the raw images.</summary>
    public void Erase()
    {
        SelfieImageS3Key = null;
        IdPhotoImageS3Key = null;
        RawImageExpiryDate = null;
        IsErased = true;
    }

    public bool HasRawImages => SelfieImageS3Key is not null && IdPhotoImageS3Key is not null;
}
