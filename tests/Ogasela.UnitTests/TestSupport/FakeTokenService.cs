using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;

namespace Ogasela.UnitTests.TestSupport;

/// <summary>
/// Deterministic stand-in for <see cref="ITokenService"/>: hashing is a pure, predictable
/// function of the input so handler tests can look tokens up by hash the same way the real
/// implementation would, without needing real JWT/crypto machinery.
/// </summary>
public sealed class FakeTokenService : ITokenService
{
    public Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(($"access-token:{user.Id}:{Guid.NewGuid()}", DateTime.UtcNow.AddMinutes(15)));

    public string GenerateRefreshTokenValue() => $"refresh-token:{Guid.NewGuid()}";

    public string HashRefreshToken(string rawToken) => $"hash:{rawToken}";
}
