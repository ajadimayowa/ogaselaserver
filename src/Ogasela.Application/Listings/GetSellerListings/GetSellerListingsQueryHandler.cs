using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.GetSellerListings;

public sealed class GetSellerListingsQueryHandler : IRequestHandler<GetSellerListingsQuery, Result<PagedListingsResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetSellerListingsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedListingsResponse>> Handle(GetSellerListingsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.Listings.Where(l => l.SellerId == request.SellerId);

        if (!await IsSameSellerAsync(request.SellerId, cancellationToken))
        {
            // Anyone browsing someone else's storefront (or an anonymous visitor) never sees Drafts.
            query = query.Where(l => l.Status != ListingStatus.Draft && l.Status != ListingStatus.PendingReview);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedListingsResponse(
            items.Select(ListingResponse.From).ToList(), page, pageSize, totalCount));
    }

    private async Task<bool> IsSameSellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return false;
        }

        return await _dbContext.SellerProfiles.AnyAsync(s => s.Id == sellerId && s.UserId == userId, cancellationToken);
    }
}
