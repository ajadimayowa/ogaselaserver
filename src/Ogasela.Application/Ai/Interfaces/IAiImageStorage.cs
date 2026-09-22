namespace Ogasela.Application.Ai.Interfaces;

/// <summary>Stores the original and enhanced images IImageEnhancer works with. Mirrors IBiometricImageStorage/ICategoryImageStorage's shape, scoped to this module.</summary>
public interface IAiImageStorage
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken);

    string GetImageUrl(string key);
}
