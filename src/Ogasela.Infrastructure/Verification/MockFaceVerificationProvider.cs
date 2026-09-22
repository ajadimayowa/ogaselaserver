using System.Text.RegularExpressions;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// A deterministic stand-in for <see cref="AwsRekognitionFaceProvider"/>, selected via
/// <c>Verification:Provider = "Mock"</c>. It never calls AWS. Every outcome is derived from
/// simple substrings embedded in the caller-supplied refs, so unit and integration tests can
/// drive every decision branch (quality failure, liveness failure, each similarity band,
/// duplicate detection) just by choosing what file name/session ref they submit - see the
/// per-method docs below for the exact flags recognised.
/// </summary>
public sealed class MockFaceVerificationProvider : IFaceVerificationProvider
{
    private static readonly Regex ScorePattern = new(@"score(\d{1,3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DuplicateSellerPattern = new(
        @"duplicate-([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled);

    /// <summary>Fallback duplicate SellerId used when a ref contains "duplicate" without an explicit id.</summary>
    public static readonly Guid DefaultDuplicateSellerId = new("11111111-1111-1111-1111-111111111111");

    public Task<string> CreateLivenessSessionAsync(CancellationToken cancellationToken) =>
        Task.FromResult($"mock-liveness-{Guid.NewGuid():N}");

    /// <summary>A ref containing "lowquality" fails the quality gate; everything else passes.</summary>
    public Task<FaceQualityResult> CheckQualityAsync(string imageRef, CancellationToken cancellationToken)
    {
        var passed = !imageRef.Contains("lowquality", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new FaceQualityResult(passed, passed ? null : "Image quality too low"));
    }

    /// <summary>A session ref containing "fail" fails liveness (score 30); everything else passes (score 98).</summary>
    public Task<LivenessResult> CheckLivenessAsync(string livenessSessionRef, CancellationToken cancellationToken)
    {
        var failed = livenessSessionRef.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(failed ? new LivenessResult(false, 30m) : new LivenessResult(true, 98m));
    }

    /// <summary>
    /// A ref containing "scoreNN" (e.g. "selfie-score65.jpg") reports NN as the similarity;
    /// otherwise similarity defaults to 96 (comfortably above the default Verified threshold).
    /// </summary>
    public Task<FaceMatchResult> CompareFacesAsync(string selfieRef, string idPhotoRef, CancellationToken cancellationToken)
    {
        var match = ScorePattern.Match(selfieRef);
        if (!match.Success)
        {
            match = ScorePattern.Match(idPhotoRef);
        }

        var similarity = match.Success ? decimal.Parse(match.Groups[1].Value) : 96m;
        return Task.FromResult(new FaceMatchResult(similarity >= 80m, similarity));
    }

    /// <summary>
    /// A ref containing "duplicate-{guid}" reports that seller id as an existing match; a ref
    /// containing bare "duplicate" reports <see cref="DefaultDuplicateSellerId"/>; otherwise null.
    /// </summary>
    public Task<Guid?> FindDuplicateAsync(string selfieRef, CancellationToken cancellationToken)
    {
        var match = DuplicateSellerPattern.Match(selfieRef);
        if (match.Success)
        {
            return Task.FromResult<Guid?>(Guid.Parse(match.Groups[1].Value));
        }

        if (selfieRef.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<Guid?>(DefaultDuplicateSellerId);
        }

        return Task.FromResult<Guid?>(null);
    }

    public Task<string> EnrollFaceAsync(string selfieRef, Guid sellerId, CancellationToken cancellationToken) =>
        Task.FromResult($"mock-face-{Guid.NewGuid():N}");

    public Task DeleteFaceAsync(string rekognitionFaceId, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
