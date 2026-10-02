using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.Analytics.GetListingAnalytics;

/// <summary>
/// One of the signed-in seller's ads: on-platform engagement for the last Days days (with the
/// previous period), and for each promoted platform the latest campaign's figures. Facebook and
/// TikTok report campaign totals since the campaign started (synced every ~20 minutes), not per day,
/// so those aren't limited to the chosen period.
/// </summary>
public sealed record GetListingAnalyticsQuery(Guid ListingId, int Days) : IRequest<Result<ListingAnalyticsResponse>>;

public sealed record ListingAnalyticsResponse(
    Guid ListingId,
    string Title,
    int Days,
    OgaselaEngagement Ogasela,
    IReadOnlyList<ExternalPlatformEngagement> Platforms);

public sealed record OgaselaEngagement(
    MetricComparison Impressions,
    MetricComparison Views,
    MetricComparison CallClicks,
    MetricComparison MessageStarts,
    MetricComparison Saves);

/// <summary>Promoted is false (and the numbers zero) for a platform this ad has never been promoted on.</summary>
public sealed record ExternalPlatformEngagement(
    AdPlatform Platform,
    bool Promoted,
    string? CampaignStatus,
    long Impressions,
    long Clicks,
    decimal SpendKobo,
    DateTime? LastSyncedAt);

public sealed class GetListingAnalyticsQueryHandler : IRequestHandler<GetListingAnalyticsQuery, Result<ListingAnalyticsResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public GetListingAnalyticsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ListingAnalyticsResponse>> Handle(GetListingAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var listing = await (
            from l in _dbContext.Listings
            join s in _dbContext.SellerProfiles on l.SellerId equals s.Id
            where l.Id == request.ListingId && s.UserId == _currentUser.UserId
            select new { l.Id, l.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (listing is null)
        {
            return Result.Failure<ListingAnalyticsResponse>(AnalyticsErrors.ListingNotFound);
        }

        var period = AnalyticsPeriod.LastDays(request.Days, _dateTime.UtcNow);
        var rows = await _dbContext.ListingDailyStats
            .Where(s => s.ListingId == listing.Id && s.Date >= period.PreviousFrom && s.Date <= period.To)
            .ToListAsync(cancellationToken);

        MetricComparison Compare(Func<Domain.Analytics.ListingDailyStat, long> value) => new(
            rows.Where(r => r.Date >= period.From).Sum(value),
            rows.Where(r => r.Date < period.From).Sum(value));

        var ogasela = new OgaselaEngagement(
            Compare(r => r.Impressions),
            Compare(r => r.Views),
            Compare(r => r.CallClicks),
            Compare(r => r.MessageStarts),
            Compare(r => r.Saves));

        var campaigns = await _dbContext.AdCampaigns
            .Where(c => c.ListingId == listing.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var platforms = Enum.GetValues<AdPlatform>()
            .Select(platform =>
            {
                var latest = campaigns.FirstOrDefault(c => c.Platform == platform);
                if (latest is null)
                {
                    return new ExternalPlatformEngagement(platform, false, null, 0, 0, 0m, null);
                }

                var metrics = ParseMetrics(latest.MetricsSnapshotJson);
                return new ExternalPlatformEngagement(
                    platform, true, latest.Status.ToString(), metrics.Impressions, metrics.Clicks, metrics.SpendKobo,
                    latest.LastMetricsSyncAt);
            })
            .ToList();

        return Result.Success(new ListingAnalyticsResponse(listing.Id, listing.Title, request.Days, ogasela, platforms));
    }

    private static AdMetricsSnapshot ParseMetrics(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }

        try
        {
            return JsonSerializer.Deserialize<AdMetricsSnapshot>(json) ?? new AdMetricsSnapshot(0, 0, 0m);
        }
        catch (JsonException)
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }
    }
}
