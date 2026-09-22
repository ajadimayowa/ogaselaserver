using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Ogasela.Application.Promotions.Interfaces;
using Ogasela.Infrastructure.Verification;

namespace Ogasela.Infrastructure.Promotions;

/// <summary>
/// Stores category/subcategory images in the same private S3 bucket Verification uses (see
/// <see cref="AwsSettings"/>), under its own "categories" prefix. Kept private rather than
/// public-read - many buckets block public ACLs by default - and served back to clients via a
/// short-lived presigned URL instead.
/// </summary>
public sealed class S3CategoryImageStorage : ICategoryImageStorage
{
    private const string KeyPrefix = "categories";
    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromHours(1);

    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _settings;

    public S3CategoryImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var key = $"{KeyPrefix}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";

        await _s3Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _settings.S3BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                AutoCloseStream = false
            },
            cancellationToken);

        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _s3Client.DeleteObjectAsync(_settings.S3BucketName, key, cancellationToken);
    }

    public string GetImageUrl(string key)
    {
        return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _settings.S3BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(PresignedUrlLifetime)
        });
    }
}
