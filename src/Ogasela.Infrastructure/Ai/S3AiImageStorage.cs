using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Infrastructure.Verification;

namespace Ogasela.Infrastructure.Ai;

/// <summary>Stores AI-tool working images (originals and enhanced copies) in the same private S3 bucket the other modules use, under its own prefix.</summary>
public sealed class S3AiImageStorage : IAiImageStorage
{
    private const string KeyPrefix = "ai-images";
    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromHours(1);

    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _settings;

    public S3AiImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken)
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

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var response = await _s3Client.GetObjectAsync(_settings.S3BucketName, key, cancellationToken);

        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public string GetImageUrl(string key) => _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
    {
        BucketName = _settings.S3BucketName,
        Key = key,
        Expires = DateTime.UtcNow.Add(PresignedUrlLifetime)
    });
}
