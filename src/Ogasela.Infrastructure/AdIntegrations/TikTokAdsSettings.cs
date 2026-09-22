namespace Ogasela.Infrastructure.AdIntegrations;

public sealed class TikTokAdsSettings
{
    public const string SectionName = "AdIntegrations:TikTok";

    public string BaseUrl { get; init; } = "https://business-api.tiktok.com/open_api/v1.3";

    public string AuthBaseUrl { get; init; } = "https://business-api.tiktok.com/portal/auth";

    public string AppId { get; init; } = string.Empty;

    public string AppSecret { get; init; } = string.Empty;

    public string RedirectUri { get; init; } = string.Empty;
}
