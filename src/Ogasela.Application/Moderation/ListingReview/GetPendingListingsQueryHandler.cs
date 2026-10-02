using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ListingReview;

public sealed class GetPendingListingsQueryHandler
    : IRequestHandler<GetPendingListingsQuery, Result<IReadOnlyList<PendingListingResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPendingListingsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<PendingListingResponse>>> Handle(
        GetPendingListingsQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
            from listing in _dbContext.Listings
            where listing.Status == ListingStatus.PendingReview
            join seller in _dbContext.SellerProfiles on listing.SellerId equals seller.Id
            join category in _dbContext.Categories on listing.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            join plan in _dbContext.PromotionPlans on listing.PromotionPlanId equals plan.Id into plans
            from plan in plans.DefaultIfEmpty()
            orderby listing.UpdatedAt
            select new { listing, seller, CategoryName = category == null ? null : category.Name, Plan = plan })
            .ToListAsync(cancellationToken);

        var listingIds = rows.Select(r => r.listing.Id).ToList();
        var fraudScores = await _dbContext.Reports
            .Where(r => r.TargetType == ReportTargetType.Listing
                && listingIds.Contains(r.TargetId)
                && r.Status == ReportStatus.Open
                && r.FraudScore != null)
            .GroupBy(r => r.TargetId)
            .Select(g => new { ListingId = g.Key, Score = g.Max(r => r.FraudScore) })
            .ToDictionaryAsync(x => x.ListingId, x => x.Score, cancellationToken);

        var response = rows.Select(r => new PendingListingResponse(
                r.listing.Id,
                r.listing.Title,
                r.listing.Description,
                r.listing.Price,
                r.listing.Condition.ToString(),
                r.listing.MediaUrls,
                r.listing.Location,
                r.CategoryName,
                r.Plan?.Name.ToString(),
                r.seller.Id,
                r.seller.BusinessName,
                r.seller.VerificationStatus.ToString(),
                fraudScores.GetValueOrDefault(r.listing.Id),
                r.listing.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<PendingListingResponse>>(response);
    }
}
