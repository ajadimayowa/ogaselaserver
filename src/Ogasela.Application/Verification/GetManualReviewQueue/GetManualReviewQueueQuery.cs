using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.GetManualReviewQueue;

public sealed record GetManualReviewQueueQuery : IRequest<Result<IReadOnlyList<ManualReviewQueueItemResponse>>>;

public sealed record ManualReviewQueueItemResponse(
    Guid VerificationId,
    Guid SellerId,
    int AttemptNumber,
    decimal? LivenessScore,
    decimal? IdMatchScore,
    DateTime CreatedAt);
