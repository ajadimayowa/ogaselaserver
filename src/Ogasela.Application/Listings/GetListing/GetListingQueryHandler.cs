using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.GetListing;

public sealed class GetListingQueryHandler : IRequestHandler<GetListingQuery, Result<ListingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetListingQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ListingResponse>> Handle(GetListingQuery request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<ListingResponse>(ListingErrors.ListingNotFound);
        }

        if (listing.Status == ListingStatus.Draft && !await IsOwnerAsync(listing, cancellationToken))
        {
            // Drafts are only visible to their own seller - hide their existence from anyone else.
            return Result.Failure<ListingResponse>(ListingErrors.ListingNotFound);
        }

        return Result.Success(ListingResponse.From(listing));
    }

    private async Task<bool> IsOwnerAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return false;
        }

        return await _dbContext.SellerProfiles
            .AnyAsync(s => s.Id == listing.SellerId && s.UserId == userId, cancellationToken);
    }
}
