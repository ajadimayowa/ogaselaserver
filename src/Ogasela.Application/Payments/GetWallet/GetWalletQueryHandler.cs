using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.GetWallet;

public sealed class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, Result<WalletResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetWalletQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<WalletResponse>> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        var sellerId = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId!.Value)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerId == Guid.Empty)
        {
            return Result.Failure<WalletResponse>(PaymentErrors.SellerProfileNotFound);
        }

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.SellerId == sellerId, cancellationToken);

        // No wallet yet just means the seller has never funded/spent anything - a zero balance, not an error.
        return Result.Success(wallet is null
            ? new WalletResponse(sellerId, 0m, DateTime.MinValue)
            : new WalletResponse(wallet.SellerId, wallet.BalanceKobo, wallet.UpdatedAt));
    }
}
