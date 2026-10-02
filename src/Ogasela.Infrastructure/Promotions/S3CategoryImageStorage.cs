using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Application.Promotions.Interfaces;
using Ogasela.Infrastructure.Verification;

namespace Ogasela.Infrastructure.Promotions;

/// <summary>
/// Private-bucket image storage shared by every admin-uploaded image type: each subclass picks its
/// own key prefix. Objects stay private (many buckets block public ACLs by default) and are served
/// back to clients via a short-lived presigned URL.
/// </summary>
public abstract class S3PrivateImageStorage
{
    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromHours(1);

    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _settings;
    private readonly string _keyPrefix;

    protected S3PrivateImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings, string keyPrefix)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _keyPrefix = keyPrefix;
    }

    public async Task<string> UploadAsync(
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var key = $"{_keyPrefix}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";

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

/// <summary>Category/subcategory images, in the same private bucket Verification uses (see <see cref="AwsSettings"/>), under "categories".</summary>
public sealed class S3CategoryImageStorage : S3PrivateImageStorage, ICategoryImageStorage
{
    public S3CategoryImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        : base(s3Client, settings, "categories")
    {
    }
}

/// <summary>Home-screen announcement images, under "announcements".</summary>
public sealed class S3AnnouncementImageStorage : S3PrivateImageStorage, IAnnouncementImageStorage
{
    public S3AnnouncementImageStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        : base(s3Client, settings, "announcements")
    {
    }
}

/// <summary>Profile photos, under "profile-photos". Same bucket the face checks read from, so a just-uploaded photo can be quality-checked in place.</summary>
public sealed class S3ProfilePhotoStorage : S3PrivateImageStorage, IProfilePhotoStorage
{
    public S3ProfilePhotoStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        : base(s3Client, settings, "profile-photos")
    {
    }
}

/// <summary>Identity documents, under "user-documents".</summary>
public sealed class S3UserDocumentStorage : S3PrivateImageStorage, IUserDocumentStorage
{
    public S3UserDocumentStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        : base(s3Client, settings, "user-documents")
    {
    }
}

/// <summary>Dispute evidence and attachments, under "dispute-evidence".</summary>
public sealed class S3DisputeEvidenceStorage : S3PrivateImageStorage, IDisputeEvidenceStorage
{
    public S3DisputeEvidenceStorage(IAmazonS3 s3Client, IOptions<AwsSettings> settings)
        : base(s3Client, settings, "dispute-evidence")
    {
    }
}
