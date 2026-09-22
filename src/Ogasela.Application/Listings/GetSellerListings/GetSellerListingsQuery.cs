using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.GetSellerListings;

public sealed record GetSellerListingsQuery(Guid SellerId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedListingsResponse>>;

public sealed record PagedListingsResponse(
    IReadOnlyList<ListingResponse> Items, int Page, int PageSize, int TotalCount);
