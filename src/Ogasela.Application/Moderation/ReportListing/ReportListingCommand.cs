using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ReportListing;

/// <summary>A user flags a listing for moderator review. Returns the report's id.</summary>
public sealed record ReportListingCommand(Guid ListingId, string Reason) : IRequest<Result<Guid>>;
