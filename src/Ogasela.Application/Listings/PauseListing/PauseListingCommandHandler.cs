using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.PauseListing;

public sealed class PauseListingCommandHandler : IRequestHandler<PauseListingCommand, Result<ListingResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public PauseListingCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<ListingResponse>> Handle(PauseListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<ListingResponse>(ListingErrors.ListingNotFound);
        }

        var isOwner = await _dbContext.SellerProfiles
            .AnyAsync(s => s.Id == listing.SellerId && s.UserId == _currentUser.UserId!.Value, cancellationToken);
        if (!isOwner)
        {
            return Result.Failure<ListingResponse>(ListingErrors.NotOwner);
        }

        if (listing.Status is not (ListingStatus.Active or ListingStatus.ExpiringSoon))
        {
            return Result.Failure<ListingResponse>(ListingErrors.InvalidStatusForPause);
        }

        listing.Pause(_dateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ListingResponse.From(listing));
    }
}
