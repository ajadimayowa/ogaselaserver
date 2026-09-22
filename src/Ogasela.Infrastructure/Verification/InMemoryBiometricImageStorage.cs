using System.Collections.Concurrent;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// Used in place of <see cref="S3BiometricImageStorage"/> whenever <c>Verification:Provider</c>
/// is "Mock" (local/dev and the automated test suites), so nothing ever needs real AWS
/// credentials to exercise the verification flow end to end. Keys preserve the original file
/// name, which is what lets <see cref="MockFaceVerificationProvider"/> read test flags (e.g.
/// "selfie-lowquality.jpg") straight out of the refs it's given.
/// </summary>
public sealed class InMemoryBiometricImageStorage : IBiometricImageStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public async Task<string> UploadAsync(
        Guid sellerId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var key = $"mock/{sellerId:N}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        _store[key] = buffer.ToArray();

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
