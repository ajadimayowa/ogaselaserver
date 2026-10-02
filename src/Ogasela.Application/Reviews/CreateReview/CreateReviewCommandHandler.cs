using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Reviews;
using Ogasela.Shared;

namespace Ogasela.Application.Reviews.CreateReview;

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<ReviewResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IPublisher _publisher;

    public CreateReviewCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime, IPublisher publisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _publisher = publisher;
    }

    public async Task<Result<ReviewResponse>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewerId = _currentUser.UserId!.Value;

        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<ReviewResponse>(ReviewErrors.ListingNotFound);
        }

        var revieweeId = await _dbContext.SellerProfiles
            .Where(s => s.Id == listing.SellerId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (revieweeId == Guid.Empty)
        {
            return Result.Failure<ReviewResponse>(ReviewErrors.SellerProfileNotFound);
        }

        if (reviewerId == revieweeId)
        {
            return Result.Failure<ReviewResponse>(ReviewErrors.CannotReviewSelf);
        }

        var alreadyReviewed = await _dbContext.Reviews.AnyAsync(
            r => r.ListingId == request.ListingId && r.ReviewerId == reviewerId, cancellationToken);

        if (alreadyReviewed)
        {
            return Result.Failure<ReviewResponse>(ReviewErrors.AlreadyReviewed);
        }

        var review = Review.Create(reviewerId, revieweeId, request.ListingId, request.Rating, request.Comment, _dateTime.UtcNow);
        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(
            new ReviewReceivedEvent(revieweeId, reviewerId, request.ListingId, request.Rating, request.Comment), cancellationToken);

        return Result.Success(new ReviewResponse(
            review.Id, review.ReviewerId, review.RevieweeId, review.ListingId, review.Rating, review.Comment, review.CreatedAt));
    }
}
