namespace Ogasela.Application.Listings.Interfaces;

/// <summary>
/// Re-encodes a seller-uploaded listing photo before it reaches IListingImageStorage: compresses
/// it and stamps a watermark identifying Ogasela and the seller who posted it, so a saved photo
/// can't be lifted and reused elsewhere without attribution.
/// </summary>
public interface IListingImageProcessor
{
    /// <summary>
    /// watermarkText is shown on the image (e.g. "Ogasela • {seller's business name}").
    /// Always re-encodes to JPEG, regardless of the input format.
    /// </summary>
    Task<ProcessedListingImage> ProcessAsync(Stream content, string watermarkText, CancellationToken cancellationToken);
}

public sealed record ProcessedListingImage(Stream Content, string ContentType, string FileExtension);
