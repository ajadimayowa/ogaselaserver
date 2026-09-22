namespace Ogasela.Application.Verification.Interfaces;

/// <summary>
/// Stores/removes the raw selfie and ID-photo images captured during biometric verification.
/// The production implementation writes to an encrypted, private S3 bucket; nothing about this
/// interface assumes S3 specifically, so tests can substitute an in-memory implementation and
/// never touch AWS.
/// </summary>
public interface IBiometricImageStorage
{
    /// <summary>Uploads raw image content for a seller and returns the storage key to reference it by.</summary>
    Task<string> UploadAsync(
        Guid sellerId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
