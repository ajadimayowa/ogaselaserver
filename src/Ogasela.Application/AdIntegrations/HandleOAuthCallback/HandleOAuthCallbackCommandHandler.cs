using MediatR;
using Ogasela.Application.AdIntegrations.Internal;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.HandleOAuthCallback;

public sealed class HandleOAuthCallbackCommandHandler : IRequestHandler<HandleOAuthCallbackCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IAdPlatformClientFactory _clientFactory;
    private readonly ITokenEncryptor _tokenEncryptor;

    public HandleOAuthCallbackCommandHandler(
        IApplicationDbContext dbContext, IDateTime dateTime, IAdPlatformClientFactory clientFactory, ITokenEncryptor tokenEncryptor)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _clientFactory = clientFactory;
        _tokenEncryptor = tokenEncryptor;
    }

    public async Task<Result> Handle(HandleOAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.State, out var sellerId))
        {
            return Result.Failure(AdIntegrationErrors.InvalidOAuthState);
        }

        var client = _clientFactory.GetClient(request.Platform);
        var callbackResult = await client.HandleOAuthCallbackAsync(request.Code, cancellationToken);
        if (callbackResult.IsFailure)
        {
            return Result.Failure(AdIntegrationErrors.OAuthCallbackFailed);
        }

        var now = _dateTime.UtcNow;

        var existing = await AdIntegrationLookups.GetActiveConnectionAsync(_dbContext, sellerId, request.Platform, cancellationToken);
        existing?.Revoke(now);

        var connection = AdAccountConnection.Create(
            Guid.NewGuid(),
            sellerId,
            request.Platform,
            _tokenEncryptor.Encrypt(callbackResult.Value.AccessToken),
            callbackResult.Value.RefreshToken is null ? null : _tokenEncryptor.Encrypt(callbackResult.Value.RefreshToken),
            callbackResult.Value.ExternalAccountId,
            now);

        _dbContext.AdAccountConnections.Add(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
