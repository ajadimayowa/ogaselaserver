namespace Ogasela.Application.Verification.Interfaces;

/// <summary>
/// Abstracts the face-verification vendor (AWS Rekognition in production, a deterministic mock
/// for local/dev and tests) behind the operations <c>SubmitVerificationCommand</c> orchestrates.
/// Every method takes an opaque reference string rather than raw image bytes: implementations
/// resolve it against wherever the raw image actually lives (e.g. an S3 key), keeping this
/// interface - and the callers that depend on it - free of storage/transport concerns.
/// </summary>
public interface IFaceVerificationProvider
{
    /// <summary>Starts a server-side AWS Face Liveness session and returns its session reference.</summary>
    Task<string> CreateLivenessSessionAsync(CancellationToken cancellationToken);

    /// <summary>Checks that a selfie image is sharp/well-lit/frontal enough to proceed.</summary>
    Task<FaceQualityResult> CheckQualityAsync(string imageRef, CancellationToken cancellationToken);

    /// <summary>Resolves a completed Face Liveness session into a pass/fail decision and score.</summary>
    Task<LivenessResult> CheckLivenessAsync(string livenessSessionRef, CancellationToken cancellationToken);

    /// <summary>Compares the selfie against the photo on the submitted ID document.</summary>
    Task<FaceMatchResult> CompareFacesAsync(string selfieRef, string idPhotoRef, CancellationToken cancellationToken);

    /// <summary>Searches the face collection for an existing enrollment that matches this selfie.</summary>
    /// <returns>The SellerId already enrolled under a matching face, or null if none was found.</returns>
    Task<Guid?> FindDuplicateAsync(string selfieRef, CancellationToken cancellationToken);

    /// <summary>Indexes the selfie into the face collection under the given seller.</summary>
    /// <returns>The vendor-assigned face identifier to store on the verification record.</returns>
    Task<string> EnrollFaceAsync(string selfieRef, Guid sellerId, CancellationToken cancellationToken);

    /// <summary>Removes a previously enrolled face from the collection (erasure/NDPA deletion).</summary>
    Task DeleteFaceAsync(string rekognitionFaceId, CancellationToken cancellationToken);
}

public sealed record FaceQualityResult(bool Passed, string? Reason);

public sealed record LivenessResult(bool Passed, decimal Score);

public sealed record FaceMatchResult(bool Passed, decimal SimilarityScore);
