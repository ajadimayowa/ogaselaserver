using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.MarkSoldListing;

public sealed record MarkSoldCommand(Guid ListingId) : IRequest<Result<ListingResponse>>;
