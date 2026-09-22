using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Payments;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.ConnectAdAccount;

public sealed class ConnectAdAccountCommandHandler : IRequestHandler<ConnectAdAccountCommand, Result<ConnectAdAccountResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAdPlatformClientFactory _clientFactory;

    public ConnectAdAccountCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IAdPlatformClientFactory clientFactory)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clientFactory = clientFactory;
    }

    public async Task<Result<ConnectAdAccountResponse>> Handle(ConnectAdAccountCommand request, CancellationToken cancellationToken)
    {
        var sellerId = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId!.Value)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerId == Guid.Empty)
        {
            return Result.Failure<ConnectAdAccountResponse>(PaymentErrors.SellerProfileNotFound);
        }

        var client = _clientFactory.GetClient(request.Platform);
        var urlResult = await client.GetOAuthUrlAsync(sellerId, cancellationToken);
        if (urlResult.IsFailure)
        {
            return Result.Failure<ConnectAdAccountResponse>(urlResult.Error);
        }

        return Result.Success(new ConnectAdAccountResponse(urlResult.Value));
    }
}
