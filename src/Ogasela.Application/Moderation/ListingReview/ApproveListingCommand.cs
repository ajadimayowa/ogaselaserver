using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ListingReview;

public sealed record ApproveListingCommand(Guid ListingId) : IRequest<Result>;

public sealed record RejectListingCommand(Guid ListingId, string Reason) : IRequest<Result>;
