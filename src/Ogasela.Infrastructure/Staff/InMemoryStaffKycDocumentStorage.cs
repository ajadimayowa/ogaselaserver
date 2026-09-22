using System.Collections.Concurrent;
using Ogasela.Application.Staff.Interfaces;

namespace Ogasela.Infrastructure.Staff;

/// <summary>
/// Used in place of <see cref="S3StaffKycDocumentStorage"/> whenever <c>Verification:Provider</c>
/// is "Mock" (local/dev and the automated test suites), so nothing ever needs real AWS
/// credentials to exercise staff onboarding end to end. Same shape as
/// <see cref="Ogasela.Infrastructure.Verification.InMemoryBiometricImageStorage"/>.
/// </summary>
public sealed class InMemoryStaffKycDocumentStorage : IStaffKycDocumentStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public async Task<string> UploadAsync(
        Guid staffProfileId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var key = $"mock/{staffProfileId:N}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        _store[key] = buffer.ToArray();

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
