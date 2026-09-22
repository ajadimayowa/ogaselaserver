using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.GetPromotionPlans;

public sealed class GetPromotionPlansQueryHandler
    : IRequestHandler<GetPromotionPlansQuery, Result<IReadOnlyList<PromotionPlanResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPromotionPlansQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<PromotionPlanResponse>>> Handle(
        GetPromotionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _dbContext.PromotionPlans
            .OrderBy(p => p.BoostWeight)
            .Select(p => new PromotionPlanResponse(
                p.Id, p.Name, p.DurationDays, p.PhotoLimit, p.VideoAllowed, p.BoostWeight, p.Price,
                p.AiToolTier, p.AdPlatformPushAllowed, p.BundledAdCreditKobo, p.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PromotionPlanResponse>>(plans);
    }
}
