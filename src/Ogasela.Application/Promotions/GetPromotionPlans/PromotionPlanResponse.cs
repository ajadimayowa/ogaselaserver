using Ogasela.Domain.Promotions;

namespace Ogasela.Application.Promotions.GetPromotionPlans;

public sealed record PromotionPlanResponse(
    Guid Id,
    PromotionPlanName Name,
    int DurationDays,
    int PhotoLimit,
    bool VideoAllowed,
    int BoostWeight,
    decimal Price,
    AiToolTier AiToolTier,
    bool AdPlatformPushAllowed,
    int BundledAdCreditKobo,
    bool IsActive);
