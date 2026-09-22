using Ogasela.Domain.Promotions;

namespace Ogasela.Api.Contracts.Promotions;

public sealed record UpdatePromotionPlanRequest(
    int DurationDays,
    int PhotoLimit,
    bool VideoAllowed,
    int BoostWeight,
    decimal Price,
    AiToolTier AiToolTier,
    bool AdPlatformPushAllowed,
    int BundledAdCreditKobo,
    bool IsActive);
