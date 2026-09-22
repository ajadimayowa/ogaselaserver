using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ogasela.Infrastructure.Verification;

namespace Ogasela.Api.HealthChecks;

/// <summary>
/// A cheap, read-only AWS connectivity probe: HeadBucket confirms the bucket exists and the
/// configured credentials can reach it, without transferring any object data or incurring a
/// meaningful cost/latency hit the way a ListObjects or GetObject call would.
/// </summary>
public sealed class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _settings;

    public S3HealthCheck(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.S3BucketName))
        {
            return HealthCheckResult.Degraded("No S3 bucket is configured.");
        }

        try
        {
            await _s3Client.HeadBucketAsync(new HeadBucketRequest { BucketName = _settings.S3BucketName }, cancellationToken);
            return HealthCheckResult.Healthy($"Bucket '{_settings.S3BucketName}' is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Bucket '{_settings.S3BucketName}' is not reachable.", ex);
        }
    }
}
