using MediatR;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.GetAdPerformance;

public sealed record GetAdPerformanceQuery(Guid ListingId) : IRequest<Result<AdPerformanceResponse>>;

public sealed record AdPerformanceResponse(
    Guid ListingId,
    long TotalImpressions,
    long TotalClicks,
    decimal TotalSpendKobo,
    IReadOnlyList<AdCampaignPerformance> Campaigns);

public sealed record AdCampaignPerformance(
    AdPlatform Platform,
    AdCampaignStatus Status,
    long Impressions,
    long Clicks,
    decimal SpendKobo,
    DateTime? LastMetricsSyncAt);
