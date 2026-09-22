using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.UpdateListing;

public sealed record UpdateListingCommand(
    Guid ListingId,
    string Title,
    string Description,
    Guid CategoryId,
    decimal? Price,
    ListingCondition Condition,
    IReadOnlyList<string> MediaUrls,
    Guid? PromotionPlanId,
    string? Location = null,
    decimal? Latitude = null,
    decimal? Longitude = null) : IRequest<Result<ListingResponse>>;
