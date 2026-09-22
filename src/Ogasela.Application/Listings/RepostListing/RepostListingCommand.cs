using MediatR;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.RepostListing;

public sealed record RepostListingCommand(Guid ListingId) : IRequest<Result<ListingResponse>>;
