namespace Ogasela.Application.Staff.Interfaces;

/// <summary>
/// Stores/removes the raw KYC document files (government ID, proof of address, passport photo)
/// uploaded during staff onboarding. Mirrors <c>IBiometricImageStorage</c>'s shape exactly - the
/// production implementation writes to a private S3 bucket, and tests substitute an in-memory
/// implementation so nothing ever needs real AWS credentials to exercise the onboarding flow.
/// </summary>
public interface IStaffKycDocumentStorage
{
    /// <summary>Uploads a KYC document's content for a staff profile and returns the storage key to reference it by.</summary>
    Task<string> UploadAsync(
        Guid staffProfileId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
