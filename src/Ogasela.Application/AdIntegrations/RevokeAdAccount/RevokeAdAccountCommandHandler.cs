using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.AdIntegrations.Internal;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.RevokeAdAccount;

public sealed class RevokeAdAccountCommandHandler : IRequestHandler<RevokeAdAccountCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IAdPlatformClientFactory _clientFactory;
    private readonly ITokenEncryptor _tokenEncryptor;

    public RevokeAdAccountCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime,
        IAdPlatformClientFactory clientFactory, ITokenEncryptor tokenEncryptor)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _clientFactory = clientFactory;
        _tokenEncryptor = tokenEncryptor;
    }

    public async Task<Result> Handle(RevokeAdAccountCommand request, CancellationToken cancellationToken)
    {
        var sellerId = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId!.Value)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var connectionResult = await AdIntegrationLookups.RequireActiveConnectionAsync(_dbContext, sellerId, request.Platform, cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure(connectionResult.Error);
        }

        var connection = connectionResult.Value;

        var client = _clientFactory.GetClient(request.Platform);
        var accessToken = _tokenEncryptor.Decrypt(connection.EncryptedAccessToken);

        var revokeResult = await client.RevokeConnectionAsync(accessToken, cancellationToken);
        if (revokeResult.IsFailure)
        {
            return Result.Failure(revokeResult.Error);
        }

        connection.Revoke(_dateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
