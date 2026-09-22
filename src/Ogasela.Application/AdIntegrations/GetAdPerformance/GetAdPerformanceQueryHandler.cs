using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.AdIntegrations.Internal;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.GetAdPerformance;

/// <summary>
/// Aggregates across whichever platform(s) a listing is promoted on. Only the most recently
/// created AdCampaign per platform counts (a listing can accumulate older, superseded rows -
/// e.g. after being ended and re-promoted).
/// </summary>
public sealed class GetAdPerformanceQueryHandler : IRequestHandler<GetAdPerformanceQuery, Result<AdPerformanceResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetAdPerformanceQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<AdPerformanceResponse>> Handle(GetAdPerformanceQuery request, CancellationToken cancellationToken)
    {
        var listingResult = await AdIntegrationLookups.GetOwnedListingAsync(_dbContext, _currentUser, request.ListingId, cancellationToken);
        if (listingResult.IsFailure)
        {
            return Result.Failure<AdPerformanceResponse>(listingResult.Error);
        }

        var allCampaigns = await _dbContext.AdCampaigns
            .Where(c => c.ListingId == request.ListingId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var latestPerPlatform = allCampaigns
            .GroupBy(c => c.Platform)
            .Select(g => g.First())
            .ToList();

        var breakdown = new List<AdCampaignPerformance>();
        long totalImpressions = 0;
        long totalClicks = 0;
        decimal totalSpendKobo = 0;

        foreach (var campaign in latestPerPlatform)
        {
            var metrics = ParseMetrics(campaign.MetricsSnapshotJson);

            breakdown.Add(new AdCampaignPerformance(
                campaign.Platform, campaign.Status, metrics.Impressions, metrics.Clicks, metrics.SpendKobo, campaign.LastMetricsSyncAt));

            totalImpressions += metrics.Impressions;
            totalClicks += metrics.Clicks;
            totalSpendKobo += metrics.SpendKobo;
        }

        return Result.Success(new AdPerformanceResponse(request.ListingId, totalImpressions, totalClicks, totalSpendKobo, breakdown));
    }

    private static AdMetricsSnapshot ParseMetrics(string? metricsSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(metricsSnapshotJson))
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }

        try
        {
            return JsonSerializer.Deserialize<AdMetricsSnapshot>(metricsSnapshotJson) ?? new AdMetricsSnapshot(0, 0, 0m);
        }
        catch (JsonException)
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }
    }
}
