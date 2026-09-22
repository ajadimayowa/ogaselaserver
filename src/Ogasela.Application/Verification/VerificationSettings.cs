namespace Ogasela.Application.Verification;

public sealed class VerificationSettings
{
    public const string SectionName = "Verification";

    /// <summary>"Aws" or "Mock" - selects the <c>IFaceVerificationProvider</c> implementation.</summary>
    public string Provider { get; init; } = "Mock";

    /// <summary>Minimum ID-match similarity (0-100) for an automatic Verified decision.</summary>
    public decimal VerifiedSimilarityThreshold { get; init; } = 90m;

    /// <summary>Minimum ID-match similarity (0-100) for an automatic ManualReview decision.</summary>
    public decimal ManualReviewSimilarityThreshold { get; init; } = 80m;

    /// <summary>Total submission attempts (across all outcomes) a seller gets before a would-be Failed result is forced to ManualReview instead.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>How long raw selfie/ID images are retained in S3 before the daily purge job deletes them.</summary>
    public int RawImageRetentionDays { get; init; } = 30;

    /// <summary>The consent copy version returned to clients and stamped onto new BiometricConsent records.</summary>
    public string CurrentConsentVersion { get; init; } = "1.0";
}
