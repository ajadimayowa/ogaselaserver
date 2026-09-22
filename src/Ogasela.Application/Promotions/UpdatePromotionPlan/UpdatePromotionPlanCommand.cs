using MediatR;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.UpdatePromotionPlan;

public sealed record UpdatePromotionPlanCommand(
    Guid Id,
    int DurationDays,
    int PhotoLimit,
    bool VideoAllowed,
    int BoostWeight,
    decimal Price,
    AiToolTier AiToolTier,
    bool AdPlatformPushAllowed,
    int BundledAdCreditKobo,
    bool IsActive) : IRequest<Result<PromotionPlanResponse>>;
