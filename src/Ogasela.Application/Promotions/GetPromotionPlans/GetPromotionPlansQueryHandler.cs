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
        // A price ladder, cheapest first - the order sellers compare plans in (not newest first).
        var plans = (await _dbContext.PromotionPlans
                .OrderBy(p => p.Price)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken))
            .Select(PromotionPlanResponse.From)
            .ToList();

        return Result.Success<IReadOnlyList<PromotionPlanResponse>>(plans);
    }
}
