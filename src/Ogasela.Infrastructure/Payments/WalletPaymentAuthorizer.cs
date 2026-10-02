using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Payments;
using Ogasela.Domain.Payments;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.Payments;

/// <summary>
/// Replaces Phase 4's StubPaymentAuthorizer: a paid plan is authorized by deducting its Price
/// directly from the seller's <see cref="Wallet"/>, recording a PlanPurchase Transaction.
/// Deliberately does not call SaveChangesAsync - the caller (ListingPublishService, via
/// PublishListingCommandHandler/RepostListingCommandHandler) persists this together with the
/// listing's own status change in a single SaveChanges call, so a wallet debit can never
/// succeed while the listing fails to publish (or vice versa).
/// </summary>
public sealed class WalletPaymentAuthorizer : IPaymentAuthorizer
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public WalletPaymentAuthorizer(IApplicationDbContext dbContext, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public async Task<Result<PaymentReceipt>> AuthorizeAsync(
        Guid sellerId, Guid promotionPlanId, Guid relatedListingId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.PromotionPlans.FirstOrDefaultAsync(p => p.Id == promotionPlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<PaymentReceipt>(PaymentErrors.PlanNotFound);
        }

        if (plan.IsFree)
        {
            return Result.Success(new PaymentReceipt(Guid.NewGuid(), sellerId, promotionPlanId, 0m, _dateTime.UtcNow));
        }

        var wallet = await _dbContext.GetOrCreateWalletAsync(sellerId, _dateTime, cancellationToken);
        if (!wallet.HasSufficientBalance(plan.Price))
        {
            return Result.Failure<PaymentReceipt>(PaymentErrors.InsufficientBalance);
        }

        var now = _dateTime.UtcNow;
        wallet.Debit(plan.Price, now);

        var transaction = Transaction.CreateSucceeded(
            sellerId, TransactionType.PlanPurchase, plan.Price, null, relatedListingId, now);
        _dbContext.Transactions.Add(transaction);

        return Result.Success(new PaymentReceipt(transaction.Id, sellerId, promotionPlanId, plan.Price, now));
    }
}
