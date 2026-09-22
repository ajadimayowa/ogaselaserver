using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.PauseListing;

public sealed record PauseListingCommand(Guid ListingId) : IRequest<Result<ListingResponse>>;
