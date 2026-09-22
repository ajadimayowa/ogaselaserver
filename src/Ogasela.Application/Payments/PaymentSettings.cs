namespace Ogasela.Application.Payments;

public sealed class PaymentSettings
{
    public const string SectionName = "Payments";

    /// <summary>"Paystack" or "Flutterwave" - selects the gateway <see cref="IPaymentGatewayResolver.GetActive"/> returns.</summary>
    public string Provider { get; init; } = "Paystack";
}
