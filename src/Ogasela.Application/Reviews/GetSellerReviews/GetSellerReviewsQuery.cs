using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Reviews.GetSellerReviews;

/// <summary>SellerId here is SellerProfile.Id, matching every other /sellers/{id}/... route.</summary>
public sealed record GetSellerReviewsQuery(Guid SellerId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedReviewsResponse>>;

public sealed record ReviewListItem(Guid Id, Guid ReviewerId, int Rating, string Comment, DateTime CreatedAt);

public sealed record PagedReviewsResponse(
    IReadOnlyList<ReviewListItem> Items, int Page, int PageSize, int TotalCount, decimal AverageRating);
