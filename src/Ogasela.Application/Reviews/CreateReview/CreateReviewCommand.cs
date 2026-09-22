using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Reviews.CreateReview;

/// <summary>RevieweeId is not client-supplied - it's resolved from the listing's seller, so a reviewer can't target an arbitrary user.</summary>
public sealed record CreateReviewCommand(Guid ListingId, int Rating, string Comment) : IRequest<Result<ReviewResponse>>;

public sealed record ReviewResponse(
    Guid Id, Guid ReviewerId, Guid RevieweeId, Guid ListingId, int Rating, string Comment, DateTime CreatedAt);
