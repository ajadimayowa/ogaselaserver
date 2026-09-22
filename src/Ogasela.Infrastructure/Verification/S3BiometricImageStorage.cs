using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// Writes raw selfie/ID-photo images to a private, encrypted S3 bucket. Objects are written
/// under a per-seller prefix with a random key component - never the seller's own identifiers
/// alone - and every object is encrypted at rest (SSE-S3). Callers are responsible for deleting
/// objects once they are no longer needed (see the daily retention job and the erasure service).
/// </summary>
public sealed class S3BiometricImageStorage : IBiometricImageStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _settings;

    public S3BiometricImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<string> UploadAsync(
        Guid sellerId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var key = $"{_settings.UploadPrefix.TrimEnd('/')}/{sellerId:N}/{Guid.NewGuid():N}-{sanitizedFileName}";

        var request = new PutObjectRequest
        {
            BucketName = _settings.S3BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await _s3Client.DeleteObjectAsync(_settings.S3BucketName, key, cancellationToken);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        Span<char> buffer = stackalloc char[name.Length];

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            buffer[i] = char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-';
        }

        return new string(buffer);
    }
}
