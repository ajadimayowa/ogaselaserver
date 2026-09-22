using Ogasela.Shared;

namespace Ogasela.Application.Payments;

/// <summary>
/// Authorizes payment for a paid <c>PromotionPlan</c> at publish/repost time.
/// <c>WalletPaymentAuthorizer</c> (Infrastructure) implements this by deducting from the
/// seller's <see cref="Domain.Payments.Wallet"/> - nothing in Listings needs to change now that
/// Phase 5 has replaced the Phase 4 stub.
/// </summary>
public interface IPaymentAuthorizer
{
    /// <param name="relatedListingId">
    /// The listing this authorization is for, recorded on the resulting PlanPurchase
    /// Transaction for traceability.
    /// </param>
    Task<Result<PaymentReceipt>> AuthorizeAsync(
        Guid sellerId, Guid promotionPlanId, Guid relatedListingId, CancellationToken cancellationToken);
}

public sealed record PaymentReceipt(Guid Id, Guid SellerId, Guid PromotionPlanId, decimal AmountKobo, DateTime AuthorizedAt);
