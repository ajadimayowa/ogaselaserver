namespace Ogasela.Infrastructure.Verification;

public sealed class RekognitionSettings
{
    public const string SectionName = "Aws:Rekognition";

    /// <summary>The Rekognition face collection sellers are enrolled/searched against.</summary>
    public string CollectionId { get; init; } = "ogasela-sellers";

    /// <summary>Minimum ID-photo/selfie similarity (0-100) Rekognition itself is asked to consider a match.</summary>
    public decimal FaceMatchThreshold { get; init; } = 90m;

    /// <summary>Similarity (0-100) above which SearchFacesByImage reports an existing enrollment as a duplicate.</summary>
    public decimal DuplicateMatchThreshold { get; init; } = 95m;
}
