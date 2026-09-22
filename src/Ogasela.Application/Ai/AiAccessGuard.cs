using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Ai;

/// <summary>
/// Gates the seller-facing AI tools by the caller's PromotionPlan.AiToolTier:
/// Basic tier -> description generation only; Standard tier -> + price suggestion;
/// Full tier -> + image enhancement and the Phase 10 ad-copy assist. AiToolTier's declaration
/// order (Basic, Standard, Full) is itself the ranking, so this is a plain ordinal comparison -
/// no separate "tier level" table to keep in sync.
/// </summary>
public sealed class AiAccessGuard
{
    private readonly IApplicationDbContext _dbContext;

    public AiAccessGuard(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PromotionPlan>> RequireCapabilityAsync(
        Guid promotionPlanId, AiCapability capability, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.PromotionPlans.FirstOrDefaultAsync(p => p.Id == promotionPlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<PromotionPlan>(AiErrors.PlanNotFound);
        }

        if ((int)plan.AiToolTier < (int)RequiredTier(capability))
        {
            return Result.Failure<PromotionPlan>(AiErrors.NotIncludedInPlan);
        }

        return Result.Success(plan);
    }

    private static AiToolTier RequiredTier(AiCapability capability) => capability switch
    {
        AiCapability.DescriptionGeneration => AiToolTier.Basic,
        AiCapability.PriceSuggestion => AiToolTier.Standard,
        AiCapability.ImageEnhancement => AiToolTier.Full,
        AiCapability.AdCopyAssist => AiToolTier.Full,
        _ => throw new ArgumentOutOfRangeException(nameof(capability), capability, null)
    };
}
