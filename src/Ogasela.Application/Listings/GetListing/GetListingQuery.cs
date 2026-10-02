using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.GetListing;

public sealed record GetListingQuery(Guid ListingId) : IRequest<Result<ListingDetailResponse>>;
