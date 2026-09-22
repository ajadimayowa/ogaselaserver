using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Reviews.GetSellerReviews;

public sealed class GetSellerReviewsQueryHandler : IRequestHandler<GetSellerReviewsQuery, Result<PagedReviewsResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSellerReviewsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedReviewsResponse>> Handle(GetSellerReviewsQuery request, CancellationToken cancellationToken)
    {
        var sellerUserId = await _dbContext.SellerProfiles
            .Where(s => s.Id == request.SellerId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerUserId == Guid.Empty)
        {
            return Result.Failure<PagedReviewsResponse>(ReviewErrors.SellerProfileNotFound);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.Reviews.Where(r => r.RevieweeId == sellerUserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var averageRating = totalCount == 0 ? 0m : await query.AverageAsync(r => (decimal)r.Rating, cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewListItem(r.Id, r.ReviewerId, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedReviewsResponse(items, page, pageSize, totalCount, Math.Round(averageRating, 2)));
    }
}
