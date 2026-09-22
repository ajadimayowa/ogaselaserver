namespace Ogasela.Domain.AdIntegrations;

/// <summary>
/// One push of a Listing to a platform's ad system. A listing may accumulate several of these
/// over time (e.g. after being reposted), so lookups by (ListingId, Platform) take the most
/// recently created row.
/// </summary>
public class AdCampaign
{
    private AdCampaign()
    {
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public AdPlatform Platform { get; private set; }

    public string ExternalCampaignId { get; private set; } = string.Empty;

    public decimal BudgetKobo { get; private set; }

    public int DurationDays { get; private set; }

    public AdCampaignStatus Status { get; private set; }

    public DateTime? LastMetricsSyncAt { get; private set; }

    /// <summary>JSON-serialized AdMetricsSnapshot (impressions/clicks/spend), normalized the same way regardless of platform.</summary>
    public string? MetricsSnapshotJson { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static AdCampaign Create(
        Guid id, Guid listingId, AdPlatform platform, string externalCampaignId,
        decimal budgetKobo, int durationDays, DateTime now)
    {
        return new AdCampaign
        {
            Id = id,
            ListingId = listingId,
            Platform = platform,
            ExternalCampaignId = externalCampaignId,
            BudgetKobo = budgetKobo,
            DurationDays = durationDays,
            Status = AdCampaignStatus.PendingReview,
            CreatedAt = now
        };
    }

    public void UpdateSyncedState(AdCampaignStatus status, string? metricsSnapshotJson, DateTime syncedAt)
    {
        Status = status;
        MetricsSnapshotJson = metricsSnapshotJson ?? MetricsSnapshotJson;
        LastMetricsSyncAt = syncedAt;
    }

    public void Pause()
    {
        Status = AdCampaignStatus.Paused;
    }

    public void Resume()
    {
        Status = AdCampaignStatus.Active;
    }

    public void End()
    {
        Status = AdCampaignStatus.Ended;
    }
}
