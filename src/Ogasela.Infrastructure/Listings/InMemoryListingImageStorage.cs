using System.Collections.Concurrent;
using Ogasela.Application.Listings.Interfaces;

namespace Ogasela.Infrastructure.Listings;

/// <summary>Used in place of <see cref="S3ListingImageStorage"/> for local/dev and tests (same "Verification:Provider" != "Aws" switch as the other storage interfaces) - no real AWS credentials needed.</summary>
public sealed class InMemoryListingImageStorage : IListingImageStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public async Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var key = $"mock/listings/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        _store[key] = buffer.ToArray();

        return $"https://mock-storage.local/{key}";
    }
}
