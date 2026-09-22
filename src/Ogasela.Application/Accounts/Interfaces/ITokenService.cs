using Ogasela.Domain.Accounts;

namespace Ogasela.Application.Accounts.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Async because, for a Staff-role user, this loads the linked StaffProfile/Role from the
    /// database to embed permission/role-type claims in the token.
    /// </summary>
    Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken);

    string GenerateRefreshTokenValue();

    string HashRefreshToken(string rawToken);
}
