using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.PublishListing;

public sealed record PublishListingCommand(Guid ListingId) : IRequest<Result<ListingResponse>>;
