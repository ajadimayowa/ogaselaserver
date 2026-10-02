namespace Ogasela.Domain.Payments;

public enum TransactionType
{
    /// <summary>
    /// Payment for a listing's PromotionPlan: either an internal wallet debit (no GatewayReference)
    /// or a card checkout through the gateway (GatewayReference set, Pending until confirmed).
    /// </summary>
    PlanPurchase,

    /// <summary>Money coming into the wallet from a gateway (Paystack/Flutterwave) checkout.</summary>
    AdSpendTopUp,

    Refund
}
