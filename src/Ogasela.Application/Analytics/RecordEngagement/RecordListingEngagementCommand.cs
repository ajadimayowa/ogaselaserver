using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Analytics.RecordEngagement;

/// <summary>
/// Engagements only the app can see happen: a tap on Call (the dialer opens outside the app) and
/// saving to favourites (stored on the device). Impressions, views and new chats are recorded
/// server-side where they happen, so they can't be sent here.
/// </summary>
public sealed record RecordListingEngagementCommand(Guid ListingId, ListingEngagement Engagement) : IRequest<Result>;

public sealed class RecordListingEngagementCommandValidator : AbstractValidator<RecordListingEngagementCommand>
{
    public RecordListingEngagementCommandValidator()
    {
        RuleFor(x => x.Engagement)
            .Must(e => e is ListingEngagement.CallClick or ListingEngagement.Save)
            .WithMessage("Only CallClick and Save can be recorded by the app.");
    }
}

public sealed class RecordListingEngagementCommandHandler : IRequestHandler<RecordListingEngagementCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IEngagementTracker _engagementTracker;

    public RecordListingEngagementCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IEngagementTracker engagementTracker)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _engagementTracker = engagementTracker;
    }

    public async Task<Result> Handle(RecordListingEngagementCommand request, CancellationToken cancellationToken)
    {
        var listing = await (
            from l in _dbContext.Listings
            join s in _dbContext.SellerProfiles on l.SellerId equals s.Id
            where l.Id == request.ListingId
                && (l.Status == ListingStatus.Active || l.Status == ListingStatus.ExpiringSoon)
            select new { l.Id, l.SellerId, SellerUserId = s.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (listing is null)
        {
            return Result.Failure(ListingErrors.ListingNotFound);
        }

        // A seller tapping Call or Save on their own ad isn't engagement; accept it silently.
        if (listing.SellerUserId != _currentUser.UserId)
        {
            await _engagementTracker.RecordAsync([(listing.Id, listing.SellerId)], request.Engagement, cancellationToken);
        }

        return Result.Success();
    }
}
