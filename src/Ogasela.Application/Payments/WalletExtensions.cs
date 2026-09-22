using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Payments;

namespace Ogasela.Application.Payments;

public static class WalletExtensions
{
    /// <summary>Wallets are created lazily - the first funding, plan purchase, or balance check for a seller creates one.</summary>
    public static async Task<Wallet> GetOrCreateWalletAsync(
        this IApplicationDbContext dbContext, Guid sellerId, IDateTime dateTime, CancellationToken cancellationToken)
    {
        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.SellerId == sellerId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = Wallet.Create(sellerId, dateTime.UtcNow);
        dbContext.Wallets.Add(wallet);
        return wallet;
    }
}
