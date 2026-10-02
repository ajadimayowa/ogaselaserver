namespace Ogasela.Application.Accounts.Interfaces;

/// <summary>Private storage for profile photos; served back as short-lived URLs.</summary>
public interface IProfilePhotoStorage
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}

/// <summary>Private storage for identity documents (ID cards, utility bills); served back as short-lived URLs.</summary>
public interface IUserDocumentStorage
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}

/// <summary>Private storage for dispute evidence (photos, PDFs); served back as short-lived URLs.</summary>
public interface IDisputeEvidenceStorage
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}
