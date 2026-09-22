namespace Ogasela.Infrastructure.Verification;

public sealed class AwsSettings
{
    public const string SectionName = "Aws";

    public string Region { get; init; } = string.Empty;

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;

    public string S3BucketName { get; init; } = string.Empty;

    /// <summary>Key prefix under the bucket that raw verification images are written to.</summary>
    public string UploadPrefix { get; init; } = "images";
}
