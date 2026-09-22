using System.Collections.Concurrent;
using Ogasela.Application.AdIntegrations;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.IntegrationTests.AdIntegrations;

/// <summary>
/// Stateful stand-in for one platform's IAdPlatformClient: campaign state lives in an in-memory
/// dictionary keyed by the fake external campaign id, so PauseCampaignAsync/ResumeCampaignAsync/
/// EndCampaignAsync (driven by our own API) and SetExternalStatus (a test-only hook simulating
/// the platform's own side, e.g. "the platform approved the campaign") both mutate the same
/// state the metrics-sync job later reads back via GetCampaignStatusAsync/GetMetricsAsync -
/// making a "poll updates status" test assert something a trivially-true fake couldn't.
/// </summary>
public sealed class FakeAdPlatformClient : IAdPlatformClient
{
    private readonly ConcurrentDictionary<string, CampaignState> _campaigns = new();

    public FakeAdPlatformClient(AdPlatform platform)
    {
        Platform = platform;
    }

    public AdPlatform Platform { get; }

    public Task<Result<string>> GetOAuthUrlAsync(Guid sellerId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success($"https://fake-{Platform}.test/oauth?state={sellerId}"));

    public Task<Result<AdOAuthCallbackResult>> HandleOAuthCallbackAsync(string code, CancellationToken cancellationToken)
    {
        if (code == "invalid-code")
        {
            return Task.FromResult(Result.Failure<AdOAuthCallbackResult>(AdIntegrationErrors.OAuthCallbackFailed));
        }

        return Task.FromResult(Result.Success(new AdOAuthCallbackResult(
            $"fake-account-{Guid.NewGuid():N}", $"fake-access-token-{code}", "fake-refresh-token")));
    }

    public Task<Result<string>> CreateCampaignAsync(AdCampaignRequest request, CancellationToken cancellationToken)
    {
        var externalCampaignId = $"fake-campaign-{Guid.NewGuid():N}";
        _campaigns[externalCampaignId] = new CampaignState(AdCampaignStatus.PendingReview, new AdMetricsSnapshot(0, 0, 0m));
        return Task.FromResult(Result.Success(externalCampaignId));
    }

    public Task<Result<AdCampaignStatus>> GetCampaignStatusAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_campaigns.TryGetValue(externalCampaignId, out var state)
            ? Result.Success(state.Status)
            : Result.Failure<AdCampaignStatus>(AdIntegrationErrors.CampaignNotFound));

    public Task<Result<AdMetricsSnapshot>> GetMetricsAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(_campaigns.TryGetValue(externalCampaignId, out var state)
            ? Result.Success(state.Metrics)
            : Result.Failure<AdMetricsSnapshot>(AdIntegrationErrors.CampaignNotFound));

    public Task<Result> PauseCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatus(externalCampaignId, AdCampaignStatus.Paused);

    public Task<Result> ResumeCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatus(externalCampaignId, AdCampaignStatus.Active);

    public Task<Result> EndCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        UpdateStatus(externalCampaignId, AdCampaignStatus.Ended);

    public Task<Result> RevokeConnectionAsync(string accessToken, CancellationToken cancellationToken) => Task.FromResult(Result.Success());

    /// <summary>Test-only hook: simulates the platform's own side changing a campaign's status/metrics (e.g. approving it), independent of any call our API makes.</summary>
    public void SetExternalStatus(string externalCampaignId, AdCampaignStatus status, AdMetricsSnapshot metrics)
    {
        _campaigns[externalCampaignId] = new CampaignState(status, metrics);
    }

    private Task<Result> UpdateStatus(string externalCampaignId, AdCampaignStatus status)
    {
        if (!_campaigns.TryGetValue(externalCampaignId, out var state))
        {
            return Task.FromResult(Result.Failure(AdIntegrationErrors.CampaignNotFound));
        }

        _campaigns[externalCampaignId] = state with { Status = status };
        return Task.FromResult(Result.Success());
    }

    private sealed record CampaignState(AdCampaignStatus Status, AdMetricsSnapshot Metrics);
}
