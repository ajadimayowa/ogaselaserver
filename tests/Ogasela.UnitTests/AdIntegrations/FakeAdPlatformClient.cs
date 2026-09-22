using Ogasela.Application.AdIntegrations;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.UnitTests.AdIntegrations;

/// <summary>Minimal stand-in for IAdPlatformClient - only CreateCampaignAsync's happy path is exercised by the plan-gating tests, everything else just needs to compile.</summary>
public sealed class FakeAdPlatformClient : IAdPlatformClient
{
    public FakeAdPlatformClient(AdPlatform platform)
    {
        Platform = platform;
    }

    public AdPlatform Platform { get; }

    public Task<Result<string>> GetOAuthUrlAsync(Guid sellerId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success("https://fake-platform.test/oauth"));

    public Task<Result<AdOAuthCallbackResult>> HandleOAuthCallbackAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new AdOAuthCallbackResult("fake-account", "fake-access-token", "fake-refresh-token")));

    public Task<Result<string>> CreateCampaignAsync(AdCampaignRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success($"fake-campaign-{Guid.NewGuid():N}"));

    public Task<Result<AdCampaignStatus>> GetCampaignStatusAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(AdCampaignStatus.Active));

    public Task<Result<AdMetricsSnapshot>> GetMetricsAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new AdMetricsSnapshot(0, 0, 0m)));

    public Task<Result> PauseCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result> ResumeCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result> EndCampaignAsync(string accessToken, string externalCampaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result> RevokeConnectionAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}
