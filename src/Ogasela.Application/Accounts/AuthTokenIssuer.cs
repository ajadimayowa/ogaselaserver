using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;

namespace Ogasela.Application.Accounts;

/// <summary>
/// Issues a fresh access/refresh token pair for a user and persists the refresh token
/// (hashed). Shared by <c>LoginCommandHandler</c> and <c>RefreshTokenCommandHandler</c> so
/// token issuance stays consistent between the two flows.
/// </summary>
public sealed class AuthTokenIssuer
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;

    public AuthTokenIssuer(IApplicationDbContext dbContext, ITokenService tokenService, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Creates and persists a new refresh token for the user, optionally recording it as the
    /// replacement for an existing one (rotation), and returns the token pair to hand back to
    /// the client.
    /// </summary>
    public async Task<AuthTokenResponse> IssueAsync(User user, RefreshToken? tokenToRotate, CancellationToken cancellationToken)
    {
        var (accessToken, accessTokenExpiresAt) = await _tokenService.GenerateAccessTokenAsync(user, cancellationToken);

        var rawRefreshToken = _tokenService.GenerateRefreshTokenValue();
        var refreshTokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiresAt = _dateTime.UtcNow.Add(RefreshTokenLifetime);

        var newRefreshToken = RefreshToken.Create(user.Id, refreshTokenHash, refreshTokenExpiresAt);
        _dbContext.RefreshTokens.Add(newRefreshToken);

        tokenToRotate?.RotateInto(newRefreshToken.Id, _dateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(accessToken, accessTokenExpiresAt, rawRefreshToken, refreshTokenExpiresAt);
    }
}
