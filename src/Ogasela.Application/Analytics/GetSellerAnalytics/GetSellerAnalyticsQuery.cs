using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Analytics.GetSellerAnalytics;

/// <summary>The signed-in seller's totals across all their ads for the last Days days, each with the previous period for comparison.</summary>
public sealed record GetSellerAnalyticsQuery(int Days) : IRequest<Result<SellerAnalyticsResponse>>;

public sealed record SellerAnalyticsResponse(
    int Days,
    DateOnly From,
    DateOnly To,
    MetricComparison ProfileVisits,
    MetricComparison Impressions,
    MetricComparison Views,
    MetricComparison CallClicks,
    MetricComparison MessageStarts,
    MetricComparison Saves);

public sealed class GetSellerAnalyticsQueryHandler : IRequestHandler<GetSellerAnalyticsQuery, Result<SellerAnalyticsResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public GetSellerAnalyticsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<SellerAnalyticsResponse>> Handle(GetSellerAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var sellerId = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerId is null)
        {
            return Result.Failure<SellerAnalyticsResponse>(AnalyticsErrors.NotASeller);
        }

        var period = AnalyticsPeriod.LastDays(request.Days, _dateTime.UtcNow);

        var listingRows = await _dbContext.ListingDailyStats
            .Where(s => s.SellerId == sellerId && s.Date >= period.PreviousFrom && s.Date <= period.To)
            .Select(s => new { s.Date, s.Impressions, s.Views, s.CallClicks, s.MessageStarts, s.Saves })
            .ToListAsync(cancellationToken);

        var visitRows = await _dbContext.SellerDailyStats
            .Where(s => s.SellerId == sellerId && s.Date >= period.PreviousFrom && s.Date <= period.To)
            .Select(s => new { s.Date, s.ProfileVisits })
            .ToListAsync(cancellationToken);

        bool IsCurrent(DateOnly date) => date >= period.From;

        MetricComparison Compare<T>(IEnumerable<T> rows, Func<T, DateOnly> date, Func<T, long> value)
        {
            var list = rows.ToList();
            return new MetricComparison(
                list.Where(r => IsCurrent(date(r))).Sum(value),
                list.Where(r => !IsCurrent(date(r))).Sum(value));
        }

        return Result.Success(new SellerAnalyticsResponse(
            request.Days,
            period.From,
            period.To,
            Compare(visitRows, r => r.Date, r => r.ProfileVisits),
            Compare(listingRows, r => r.Date, r => r.Impressions),
            Compare(listingRows, r => r.Date, r => r.Views),
            Compare(listingRows, r => r.Date, r => r.CallClicks),
            Compare(listingRows, r => r.Date, r => r.MessageStarts),
            Compare(listingRows, r => r.Date, r => r.Saves)));
    }
}
