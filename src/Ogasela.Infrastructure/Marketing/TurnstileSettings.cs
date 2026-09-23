namespace Ogasela.Infrastructure.Marketing;

public sealed class TurnstileSettings
{
    public const string SectionName = "Turnstile";

    public string BaseUrl { get; init; } = "https://challenges.cloudflare.com/turnstile/v0/";

    public string SecretKey { get; init; } = string.Empty;
}
