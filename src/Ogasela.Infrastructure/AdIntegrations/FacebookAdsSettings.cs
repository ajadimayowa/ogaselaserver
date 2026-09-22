namespace Ogasela.Infrastructure.AdIntegrations;

public sealed class FacebookAdsSettings
{
    public const string SectionName = "AdIntegrations:Facebook";

    public string BaseUrl { get; init; } = "https://graph.facebook.com/v21.0";

    public string AppId { get; init; } = string.Empty;

    public string AppSecret { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;
}
