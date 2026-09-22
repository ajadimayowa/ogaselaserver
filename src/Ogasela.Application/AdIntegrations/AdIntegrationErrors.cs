using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations;

public static class AdIntegrationErrors
{
    public static readonly Error ConnectionNotFound = new(
        "AdIntegrations.ConnectionNotFound", "No ad account connection was found for this platform. Connect one first.");

    public static readonly Error ConnectionRevoked = new(
        "AdIntegrations.ConnectionRevoked", "This ad account connection has been revoked. Reconnect to continue.");

    public static readonly Error CampaignNotFound = new(
        "AdIntegrations.CampaignNotFound", "No ad campaign was found for this listing on this platform.");

    public static readonly Error PromotionNotAllowed = new(
        "AdIntegrations.PromotionNotAllowed", "Your current promotion plan does not include ad platform promotion.");

    public static readonly Error InvalidOAuthState = new(
        "AdIntegrations.InvalidOAuthState", "The OAuth callback state could not be matched to a seller.");

    public static readonly Error OAuthCallbackFailed = new(
        "AdIntegrations.OAuthCallbackFailed", "The ad platform rejected the OAuth callback.");

    public static readonly Error PlatformRequestFailed = new(
        "AdIntegrations.PlatformRequestFailed", "The ad platform could not complete this request right now. Try again shortly.");
}
