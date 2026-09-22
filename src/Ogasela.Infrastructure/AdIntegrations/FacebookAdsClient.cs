using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogasela.Application.AdIntegrations;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.AdIntegrations;

/// <summary>
/// <see cref="IAdPlatformClient"/> backed by Meta's Marketing API
/// (https://developers.facebook.com/docs/marketing-api). Auth is the standard Facebook Login
/// OAuth dialog; ad account/campaign operations go through the Graph API's act_{id} node.
/// </summary>
public sealed class FacebookAdsClient : IAdPlatformClient
{
    private readonly HttpClient _httpClient;
    private readonly FacebookAdsSettings _settings;
    private readonly ILogger<FacebookAdsClient> _logger;

    public FacebookAdsClient(HttpClient httpClient, IOptions<FacebookAdsSettings> settings, ILogger<FacebookAdsClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl + "/");
    }

    public AdPlatform Platform => AdPlatform.Facebook;

    public Task<Result<string>> GetOAuthUrlAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId))
        {
            _logger.LogWarning("Facebook Ads is not configured; cannot build an OAuth URL");
            return Task.FromResult(Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed));
        }

        var url = "https://www.facebook.com/v21.0/dialog/oauth"
            + $"?client_id={Uri.EscapeDataString(_settings.AppId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}"
            + $"&state={Uri.EscapeDataString(sellerId.ToString())}"
            + "&scope=ads_management,business_management";

        return Task.FromResult(Result.Success(url));
    }

    public async Task<Result<AdOAuthCallbackResult>> HandleOAuthCallbackAsync(string code, CancellationToken cancellationToken)
    {
        var tokenUrl = "oauth/access_token"
            + $"?client_id={Uri.EscapeDataString(_settings.AppId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}"
            + $"&client_secret={Uri.EscapeDataString(_settings.AppSecret)}"
            + $"&code={Uri.EscapeDataString(code)}";

        using var tokenResponse = await _httpClient.GetAsync(tokenUrl, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook OAuth token exchange failed with {StatusCode}: {Body}", (int)tokenResponse.StatusCode, tokenBody);
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        var token = JsonSerializer.Deserialize<AccessTokenResponse>(tokenBody, JsonOptions);
        if (token?.AccessToken is null)
        {
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        using var accountsResponse = await _httpClient.GetAsync(
            $"me/adaccounts?access_token={Uri.EscapeDataString(token.AccessToken)}", cancellationToken);
        var accountsBody = await accountsResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!accountsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook ad account lookup failed with {StatusCode}: {Body}", (int)accountsResponse.StatusCode, accountsBody);
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        var accounts = JsonSerializer.Deserialize<AdAccountsResponse>(accountsBody, JsonOptions);
        var externalAccountId = accounts?.Data.FirstOrDefault()?.Id;
        if (string.IsNullOrWhiteSpace(externalAccountId))
        {
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        // Facebook long-lived tokens don't come with a separate refresh token the way TikTok's do.
        return Result.Success(new AdOAuthCallbackResult(externalAccountId, token.AccessToken, RefreshToken: null));
    }

    public async Task<Result<string>> CreateCampaignAsync(AdCampaignRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            name = request.Title,
            objective = "OUTCOME_TRAFFIC",
            status = "PAUSED",
            special_ad_categories = Array.Empty<string>(),
            daily_budget = (long)(request.BudgetKobo / 100m),
            access_token = request.AccessToken
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{request.ExternalAccountId}/campaigns", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook campaign creation failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<CreateCampaignResponse>(body, JsonOptions);
        if (string.IsNullOrWhiteSpace(parsed?.Id))
        {
            return Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success(parsed.Id);
    }

    public async Task<Result<AdCampaignStatus>> GetCampaignStatusAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{externalCampaignId}?fields=effective_status&access_token={Uri.EscapeDataString(accessToken)}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook campaign status lookup failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<AdCampaignStatus>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<CampaignStatusResponse>(body, JsonOptions);
        return Result.Success(MapStatus(parsed?.EffectiveStatus));
    }

    public async Task<Result<AdMetricsSnapshot>> GetMetricsAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{externalCampaignId}/insights?fields=impressions,clicks,spend&access_token={Uri.EscapeDataString(accessToken)}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook metrics lookup failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<AdMetricsSnapshot>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<InsightsResponse>(body, JsonOptions);
        var insight = parsed?.Data.FirstOrDefault();
        if (insight is null)
        {
            return Result.Success(new AdMetricsSnapshot(0, 0, 0m));
        }

        var spendKobo = decimal.TryParse(insight.Spend, out var spendNaira) ? spendNaira * 100m : 0m;
        return Result.Success(new AdMetricsSnapshot(insight.Impressions, insight.Clicks, spendKobo));
    }

    public Task<Result> PauseCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "PAUSED", cancellationToken);

    public Task<Result> ResumeCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "ACTIVE", cancellationToken);

    public Task<Result> EndCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "ARCHIVED", cancellationToken);

    public async Task<Result> RevokeConnectionAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(
            $"me/permissions?access_token={Uri.EscapeDataString(accessToken)}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Facebook permission revoke failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success();
    }

    private async Task<Result> UpdateStatusAsync(string accessToken, string externalCampaignId, string status, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{externalCampaignId}", new { status, access_token = accessToken }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Facebook campaign status update to {Status} failed with {StatusCode}: {Body}", status, (int)response.StatusCode, body);
            return Result.Failure(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success();
    }

    private static AdCampaignStatus MapStatus(string? effectiveStatus) => effectiveStatus switch
    {
        "ACTIVE" => AdCampaignStatus.Active,
        "PAUSED" or "CAMPAIGN_PAUSED" or "ADSET_PAUSED" => AdCampaignStatus.Paused,
        "DISAPPROVED" => AdCampaignStatus.Rejected,
        "ARCHIVED" or "DELETED" => AdCampaignStatus.Ended,
        _ => AdCampaignStatus.PendingReview
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record AccessTokenResponse([property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record AdAccountsResponse([property: JsonPropertyName("data")] List<AdAccountData> Data);

    private sealed record AdAccountData([property: JsonPropertyName("id")] string Id);

    private sealed record CreateCampaignResponse([property: JsonPropertyName("id")] string? Id);

    private sealed record CampaignStatusResponse([property: JsonPropertyName("effective_status")] string? EffectiveStatus);

    private sealed record InsightsResponse([property: JsonPropertyName("data")] List<InsightData> Data);

    private sealed record InsightData(
        [property: JsonPropertyName("impressions")] long Impressions,
        [property: JsonPropertyName("clicks")] long Clicks,
        [property: JsonPropertyName("spend")] string? Spend);
}
