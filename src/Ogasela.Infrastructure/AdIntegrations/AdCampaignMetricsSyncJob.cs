using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Infrastructure.AdIntegrations;

/// <summary>
/// Hangfire recurring job (every 15-30 min, see DependencyInjection/Program) polling every
/// Active or PendingReview AdCampaign row via GetCampaignStatusAsync/GetMetricsAsync and
/// updating Status/MetricsSnapshotJson. Per-call exponential backoff and circuit-breaking on
/// rate-limit responses lives on the FacebookAdsClient/TikTokAdsClient HttpClients themselves
/// (Microsoft.Extensions.Http.Resilience's AddStandardResilienceHandler, built on Polly - see
/// AddAdIntegrations), not here: a failed call below has already exhausted its retries by the
/// time it reaches this loop, so a single platform outage just leaves that campaign's row
/// unchanged for this cycle rather than throwing and skipping every other campaign after it.
/// </summary>
public sealed class AdCampaignMetricsSyncJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IAdPlatformClientFactory _clientFactory;
    private readonly ITokenEncryptor _tokenEncryptor;
    private readonly ILogger<AdCampaignMetricsSyncJob> _logger;

    public AdCampaignMetricsSyncJob(
        IApplicationDbContext dbContext, IDateTime dateTime, IAdPlatformClientFactory clientFactory,
        ITokenEncryptor tokenEncryptor, ILogger<AdCampaignMetricsSyncJob> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _clientFactory = clientFactory;
        _tokenEncryptor = tokenEncryptor;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = _dateTime.UtcNow;

        var syncable = await _dbContext.AdCampaigns
            .Where(c => c.Status == AdCampaignStatus.Active || c.Status == AdCampaignStatus.PendingReview)
            .ToListAsync();

        if (syncable.Count == 0)
        {
            return;
        }

        var listingIds = syncable.Select(c => c.ListingId).Distinct().ToList();
        var sellerIdsByListing = await _dbContext.Listings
            .Where(l => listingIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.SellerId);

        var connectionCache = new Dictionary<(Guid SellerId, AdPlatform Platform), AdAccountConnection?>();
        var updatedCount = 0;

        foreach (var campaign in syncable)
        {
            if (!sellerIdsByListing.TryGetValue(campaign.ListingId, out var sellerId))
            {
                continue;
            }

            var cacheKey = (sellerId, campaign.Platform);
            if (!connectionCache.TryGetValue(cacheKey, out var connection))
            {
                connection = await _dbContext.AdAccountConnections
                    .Where(c => c.SellerId == sellerId && c.Platform == campaign.Platform && c.RevokedAt == null)
                    .OrderByDescending(c => c.ConnectedAt)
                    .FirstOrDefaultAsync();
                connectionCache[cacheKey] = connection;
            }

            if (connection is null)
            {
                // Revoked since the campaign was created/last synced - nothing left to poll with.
                continue;
            }

            var client = _clientFactory.GetClient(campaign.Platform);
            var accessToken = _tokenEncryptor.Decrypt(connection.EncryptedAccessToken);

            var statusResult = await client.GetCampaignStatusAsync(accessToken, campaign.ExternalCampaignId, CancellationToken.None);
            if (statusResult.IsFailure)
            {
                _logger.LogWarning(
                    "Ad metrics sync: status lookup failed for campaign {CampaignId} on {Platform}: {ErrorCode}",
                    campaign.Id, campaign.Platform, statusResult.Error.Code);
                continue;
            }

            var metricsResult = await client.GetMetricsAsync(accessToken, campaign.ExternalCampaignId, CancellationToken.None);
            var metricsJson = metricsResult.IsSuccess ? JsonSerializer.Serialize(metricsResult.Value) : null;

            campaign.UpdateSyncedState(statusResult.Value, metricsJson, now);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation("Ad metrics sync: updated {Count} campaign(s)", updatedCount);
        }
    }
}
