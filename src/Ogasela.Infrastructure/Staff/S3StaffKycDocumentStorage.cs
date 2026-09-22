using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Ogasela.Application.Staff.Interfaces;
using Ogasela.Infrastructure.Verification;

namespace Ogasela.Infrastructure.Staff;

/// <summary>
/// Writes raw staff KYC document files to a private, encrypted S3 bucket. Objects are written
/// under a per-staff-profile prefix with a random key component and encrypted at rest (SSE-S3),
/// the same shape as <see cref="S3BiometricImageStorage"/>. Reuses the same <see cref="AwsSettings"/>
/// bucket (only the key prefix differs, via <see cref="StaffKycSettings"/>).
/// </summary>
public sealed class S3StaffKycDocumentStorage : IStaffKycDocumentStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _awsSettings;
    private readonly StaffKycSettings _staffKycSettings;

    public S3StaffKycDocumentStorage(IAmazonS3 s3Client, IOptions<AwsSettings> awsSettings, IOptions<StaffKycSettings> staffKycSettings)
    {
        _s3Client = s3Client;
        _awsSettings = awsSettings.Value;
        _staffKycSettings = staffKycSettings.Value;
    }

    public async Task<string> UploadAsync(
        Guid staffProfileId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var key = $"{_staffKycSettings.UploadPrefix.TrimEnd('/')}/{staffProfileId:N}/{Guid.NewGuid():N}-{sanitizedFileName}";

        var request = new PutObjectRequest
        {
            BucketName = _awsSettings.S3BucketName,
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
        await _s3Client.DeleteObjectAsync(_awsSettings.S3BucketName, key, cancellationToken);
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
