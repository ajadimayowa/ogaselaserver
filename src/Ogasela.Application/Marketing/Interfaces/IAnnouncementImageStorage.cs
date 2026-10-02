namespace Ogasela.Application.Marketing.Interfaces;

/// <summary>Private storage for announcement images, served back as short-lived URLs (same approach as category images).</summary>
public interface IAnnouncementImageStorage
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}
