namespace Ogasela.Application.Promotions.Interfaces;

/// <summary>
/// Stores the image representing a category/subcategory. Unlike biometric images, these are
/// meant to be shown to any client, so <see cref="GetImageUrl"/> hands back a time-limited
/// presigned URL rather than requiring the bucket itself to be public.
/// </summary>
public interface ICategoryImageStorage
{
    /// <summary>Uploads the image and returns the storage key to persist on the Category.</summary>
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}
