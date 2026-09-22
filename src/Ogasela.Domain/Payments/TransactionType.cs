namespace Ogasela.Domain.Payments;

public enum TransactionType
{
    /// <summary>An internal wallet debit to pay for a paid PromotionPlan - never touches a gateway.</summary>
    PlanPurchase,

    /// <summary>Money coming into the wallet from a gateway (Paystack/Flutterwave) checkout.</summary>
    AdSpendTopUp,

    Refund
}
