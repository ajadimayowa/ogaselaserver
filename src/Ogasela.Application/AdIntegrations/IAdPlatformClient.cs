using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations;

/// <summary>
/// A thin adapter over one ad platform's own API (Meta Marketing API for Facebook, TikTok
/// Business API for TikTok). Deliberately stateless - like every other external-integration
/// interface in this codebase (IPaymentGateway, IFaceVerificationProvider), it never touches
/// IApplicationDbContext itself; the calling command handler is responsible for persisting
/// whatever the client returns (an AdAccountConnection, an AdCampaign, etc).
/// </summary>
public interface IAdPlatformClient
{
    AdPlatform Platform { get; }

    /// <summary>
    /// Builds the platform's OAuth authorization URL for this seller to visit. The seller id is
    /// round-tripped through OAuth's "state" parameter so the callback (which a stateless
    /// JWT-bearer API can't protect with [Authorize], since the platform's redirect carries no
    /// bearer token) can be tied back to the seller who started the flow.
    /// </summary>
    Task<Result<string>> GetOAuthUrlAsync(Guid sellerId, CancellationToken cancellationToken);

    /// <summary>Exchanges an OAuth authorization code for a long-lived access (and, where the platform issues one, refresh) token.</summary>
    Task<Result<AdOAuthCallbackResult>> HandleOAuthCallbackAsync(string code, CancellationToken cancellationToken);

    Task<Result<string>> CreateCampaignAsync(AdCampaignRequest request, CancellationToken cancellationToken);

    Task<Result<AdCampaignStatus>> GetCampaignStatusAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken);

    Task<Result<AdMetricsSnapshot>> GetMetricsAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken);

    Task<Result> PauseCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken);

    Task<Result> ResumeCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken);

    Task<Result> EndCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the platform-side authorization. Takes the decrypted access token rather than a
    /// sellerId, matching every other method here - the stateless client has no DB access to
    /// resolve one to the other itself, so the command handler decrypts and passes it in.
    /// </summary>
    Task<Result> RevokeConnectionAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed record AdOAuthCallbackResult(string ExternalAccountId, string AccessToken, string? RefreshToken);

public sealed record AdCampaignRequest(
    Guid ListingId,
    string AccessToken,
    string ExternalAccountId,
    string Title,
    string Description,
    IReadOnlyList<string> MediaUrls,
    decimal BudgetKobo,
    int DurationDays,
    string? Audience);

/// <summary>Normalized shape every platform's own metrics response is mapped into, so GetAdPerformanceQuery can sum across platforms uniformly.</summary>
public sealed record AdMetricsSnapshot(long Impressions, long Clicks, decimal SpendKobo);
