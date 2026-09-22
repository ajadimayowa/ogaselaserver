namespace Ogasela.Infrastructure.Payments;

public sealed class FlutterwaveSettings
{
    public const string SectionName = "Payments:Flutterwave";

    public string BaseUrl { get; init; } = "https://api.flutterwave.com/v3";

    public string SecretKey { get; init; } = string.Empty;

    /// <summary>The pre-shared hash configured in the Flutterwave dashboard, compared against the "verif-hash" webhook header.</summary>
    public string SecretHash { get; init; } = string.Empty;

    public string RedirectUrl { get; init; } = string.Empty;
}
