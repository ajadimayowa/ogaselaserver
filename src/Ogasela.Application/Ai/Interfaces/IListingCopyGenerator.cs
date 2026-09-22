using Ogasela.Shared;

namespace Ogasela.Application.Ai.Interfaces;

/// <summary>Generates a suggested title/description from either free-text keywords or a reference photo (at least one must be given).</summary>
public interface IListingCopyGenerator
{
    Task<Result<ListingCopySuggestion>> GenerateDescriptionAsync(
        string? keywords, string? imageRef, CancellationToken cancellationToken);
}

public sealed record ListingCopySuggestion(string Title, string Description);
