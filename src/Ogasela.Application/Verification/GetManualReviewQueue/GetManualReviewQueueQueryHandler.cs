using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Verification;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.GetManualReviewQueue;

/// <summary>Just the Phase 2 manual-review queue on its own, oldest first - the unified, priority-scored view across Reports too lives at Phase 11's GetModerationQueueQuery.</summary>
public sealed class GetManualReviewQueueQueryHandler
    : IRequestHandler<GetManualReviewQueueQuery, Result<IReadOnlyList<ManualReviewQueueItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetManualReviewQueueQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ManualReviewQueueItemResponse>>> Handle(
        GetManualReviewQueueQuery request, CancellationToken cancellationToken)
    {
        var items = await _dbContext.BiometricVerifications
            .Where(v => v.Decision == VerificationDecision.ManualReview)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new ManualReviewQueueItemResponse(
                v.Id, v.SellerId, v.AttemptNumber, v.LivenessScore, v.IdMatchScore, v.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ManualReviewQueueItemResponse>>(items);
    }
}
