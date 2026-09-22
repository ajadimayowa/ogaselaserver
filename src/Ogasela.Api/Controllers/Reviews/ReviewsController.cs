using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Reviews;
using Ogasela.Application.Reviews.CreateReview;
using Ogasela.Application.Reviews.GetSellerReviews;

namespace Ogasela.Api.Controllers.Reviews;

[ApiController]
public sealed class ReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Leaves a 1-5 star review on a listing's seller. One review per (buyer, listing) pair; the seller can't review their own listing.</summary>
    [HttpPost("api/v1/reviews")]
    [Authorize]
    public async Task<IActionResult> Create(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(request.ListingId, request.Rating, request.Comment);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Public - a seller's reviews, newest first. id is the SellerProfile id.</summary>
    [HttpGet("api/v1/sellers/{id:guid}/reviews")]
    public async Task<IActionResult> GetSellerReviews(
        Guid id, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new GetSellerReviewsQuery(id, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
