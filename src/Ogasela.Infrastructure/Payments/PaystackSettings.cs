namespace Ogasela.Infrastructure.Payments;

public sealed class PaystackSettings
{
    public const string SectionName = "Payments:Paystack";

    public string BaseUrl { get; init; } = "https://api.paystack.co";

    public string SecretKey { get; init; } = string.Empty;

    public string CallbackUrl { get; init; } = string.Empty;
}
