using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;
    private readonly AuthTokenIssuer _tokenIssuer;

    public RefreshTokenCommandHandler(
        IApplicationDbContext dbContext,
        ITokenService tokenService,
        IDateTime dateTime,
        AuthTokenIssuer tokenIssuer)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _dateTime = dateTime;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<AuthTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.RefreshTokenInvalid);
        }

        if (existingToken.IsRevoked)
        {
            if (existingToken.WasRotated)
            {
                // The token was legitimately rotated once already; presenting it again means
                // it was stolen or copied. Revoke every other active token for this user.
                await RevokeAllActiveTokensAsync(existingToken.UserId, cancellationToken);
                return Result.Failure<AuthTokenResponse>(AccountErrors.RefreshTokenReused);
            }

            return Result.Failure<AuthTokenResponse>(AccountErrors.RefreshTokenInvalid);
        }

        if (existingToken.ExpiresAt <= _dateTime.UtcNow)
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.RefreshTokenExpired);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.UserNotFound);
        }

        var response = await _tokenIssuer.IssueAsync(user, existingToken, cancellationToken);
        return Result.Success(response);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(_dateTime.UtcNow);
        }

        if (activeTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
