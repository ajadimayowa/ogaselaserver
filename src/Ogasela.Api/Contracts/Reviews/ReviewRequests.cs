namespace Ogasela.Api.Contracts.Reviews;

public sealed record CreateReviewRequest(Guid ListingId, int Rating, string Comment);
