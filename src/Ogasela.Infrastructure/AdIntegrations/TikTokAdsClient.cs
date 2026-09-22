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
/// <see cref="IAdPlatformClient"/> backed by the TikTok Business API
/// (https://business-api.tiktok.com/portal/docs). Unlike Facebook, TikTok's token exchange
/// returns both an access token and a refresh token, and every write call is a POST with the
/// access token in an "Access-Token" header rather than a query parameter.
/// </summary>
public sealed class TikTokAdsClient : IAdPlatformClient
{
    private readonly HttpClient _httpClient;
    private readonly TikTokAdsSettings _settings;
    private readonly ILogger<TikTokAdsClient> _logger;

    public TikTokAdsClient(HttpClient httpClient, IOptions<TikTokAdsSettings> settings, ILogger<TikTokAdsClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl + "/");
    }

    public AdPlatform Platform => AdPlatform.TikTok;

    public Task<Result<string>> GetOAuthUrlAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AppId))
        {
            _logger.LogWarning("TikTok Ads is not configured; cannot build an OAuth URL");
            return Task.FromResult(Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed));
        }

        var url = _settings.AuthBaseUrl
            + $"?app_id={Uri.EscapeDataString(_settings.AppId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}"
            + $"&state={Uri.EscapeDataString(sellerId.ToString())}";

        return Task.FromResult(Result.Success(url));
    }

    public async Task<Result<AdOAuthCallbackResult>> HandleOAuthCallbackAsync(string code, CancellationToken cancellationToken)
    {
        var payload = new
        {
            app_id = _settings.AppId,
            secret = _settings.AppSecret,
            auth_code = code
        };

        using var response = await _httpClient.PostAsJsonAsync("oauth2/access_token/", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TikTok OAuth token exchange failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        var parsed = JsonSerializer.Deserialize<TikTokEnvelope<AccessTokenData>>(body, JsonOptions);
        var data = parsed?.Data;
        var advertiserId = data?.AdvertiserIds?.FirstOrDefault();

        if (data?.AccessToken is null || string.IsNullOrWhiteSpace(advertiserId))
        {
            return Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed);
        }

        return Result.Success(new AdOAuthCallbackResult(advertiserId, data.AccessToken, data.RefreshToken));
    }

    public async Task<Result<string>> CreateCampaignAsync(AdCampaignRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            advertiser_id = request.ExternalAccountId,
            campaign_name = request.Title,
            objective_type = "TRAFFIC",
            budget_mode = "BUDGET_MODE_TOTAL",
            budget = request.BudgetKobo / 100m
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "campaign/create/")
        {
            Content = JsonContent.Create(payload)
        };
        requestMessage.Headers.Add("Access-Token", request.AccessToken);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TikTok campaign creation failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<TikTokEnvelope<CampaignIdData>>(body, JsonOptions);
        if (string.IsNullOrWhiteSpace(parsed?.Data?.CampaignId))
        {
            return Result.Failure<string>(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success(parsed.Data.CampaignId);
    }

    public async Task<Result<AdCampaignStatus>> GetCampaignStatusAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(
            HttpMethod.Get, $"campaign/get/?campaign_ids=[\"{externalCampaignId}\"]");
        requestMessage.Headers.Add("Access-Token", accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TikTok campaign status lookup failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<AdCampaignStatus>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<TikTokEnvelope<CampaignListData>>(body, JsonOptions);
        var status = parsed?.Data?.List.FirstOrDefault()?.OperationStatus;
        return Result.Success(MapStatus(status));
    }

    public async Task<Result<AdMetricsSnapshot>> GetMetricsAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken)
    {
        var payload = new
        {
            advertiser_id = externalCampaignId,
            report_type = "BASIC",
            dimensions = new[] { "campaign_id" },
            metrics = new[] { "impressions", "clicks", "spend" }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "report/integrated/get/")
        {
            Content = JsonContent.Create(payload)
        };
        requestMessage.Headers.Add("Access-Token", accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TikTok metrics lookup failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure<AdMetricsSnapshot>(AdIntegrationErrors.PlatformRequestFailed);
        }

        var parsed = JsonSerializer.Deserialize<TikTokEnvelope<ReportData>>(body, JsonOptions);
        var metrics = parsed?.Data?.List.FirstOrDefault()?.Metrics;
        if (metrics is null)
        {
            return Result.Success(new AdMetricsSnapshot(0, 0, 0m));
        }

        var spendKobo = decimal.TryParse(metrics.Spend, out var spendNaira) ? spendNaira * 100m : 0m;
        var impressions = long.TryParse(metrics.Impressions, out var impressionsValue) ? impressionsValue : 0;
        var clicks = long.TryParse(metrics.Clicks, out var clicksValue) ? clicksValue : 0;

        return Result.Success(new AdMetricsSnapshot(impressions, clicks, spendKobo));
    }

    public Task<Result> PauseCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "DISABLE", cancellationToken);

    public Task<Result> ResumeCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "ENABLE", cancellationToken);

    public Task<Result> EndCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatusAsync(accessToken, externalCampaignId, "DELETE", cancellationToken);

    public async Task<Result> RevokeConnectionAsync(string accessToken, CancellationToken cancellationToken)
    {
        var payload = new { app_id = _settings.AppId, secret = _settings.AppSecret, access_token = accessToken };

        using var response = await _httpClient.PostAsJsonAsync("oauth2/revoke_token/", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("TikTok token revoke failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            return Result.Failure(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success();
    }

    private async Task<Result> UpdateStatusAsync(string accessToken, string externalCampaignId, string operationStatus, CancellationToken cancellationToken)
    {
        var payload = new { campaign_ids = new[] { externalCampaignId }, operation_status = operationStatus };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "campaign/status/update/")
        {
            Content = JsonContent.Create(payload)
        };
        requestMessage.Headers.Add("Access-Token", accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("TikTok campaign status update to {Status} failed with {StatusCode}: {Body}", operationStatus, (int)response.StatusCode, body);
            return Result.Failure(AdIntegrationErrors.PlatformRequestFailed);
        }

        return Result.Success();
    }

    private static AdCampaignStatus MapStatus(string? operationStatus) => operationStatus switch
    {
        "ENABLE" => AdCampaignStatus.Active,
        "DISABLE" => AdCampaignStatus.Paused,
        "DELETE" => AdCampaignStatus.Ended,
        "AUDIT_DENY" => AdCampaignStatus.Rejected,
        _ => AdCampaignStatus.PendingReview
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record TikTokEnvelope<T>([property: JsonPropertyName("data")] T? Data);

    private sealed record AccessTokenData(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("advertiser_ids")] List<string>? AdvertiserIds);

    private sealed record CampaignIdData([property: JsonPropertyName("campaign_id")] string? CampaignId);

    private sealed record CampaignListData([property: JsonPropertyName("list")] List<CampaignListItem> List);

    private sealed record CampaignListItem([property: JsonPropertyName("operation_status")] string? OperationStatus);

    private sealed record ReportData([property: JsonPropertyName("list")] List<ReportListItem> List);

    private sealed record ReportListItem([property: JsonPropertyName("metrics")] ReportMetrics? Metrics);

    private sealed record ReportMetrics(
        [property: JsonPropertyName("impressions")] string? Impressions,
        [property: JsonPropertyName("clicks")] string? Clicks,
        [property: JsonPropertyName("spend")] string? Spend);
}
