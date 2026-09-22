using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IDateTime _dateTime;

    public LogoutCommandHandler(IApplicationDbContext dbContext, ITokenService tokenService, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || existingToken.IsRevoked)
        {
            return Result.Failure(AccountErrors.RefreshTokenInvalid);
        }

        existingToken.Revoke(_dateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
