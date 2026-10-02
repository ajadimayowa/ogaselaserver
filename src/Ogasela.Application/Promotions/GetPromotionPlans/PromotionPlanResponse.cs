using Ogasela.Domain.Promotions;

namespace Ogasela.Application.Promotions.GetPromotionPlans;

public sealed record PromotionPlanResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Features,
    int DurationDays,
    int PhotoLimit,
    bool VideoAllowed,
    int BoostWeight,
    decimal Price,
    AiToolTier AiToolTier,
    bool AdPlatformPushAllowed,
    int BundledAdCreditKobo,
    bool IsActive)
{
    public static PromotionPlanResponse From(PromotionPlan p) => new(
        p.Id, p.Name, p.Description, p.Features, p.DurationDays, p.PhotoLimit, p.VideoAllowed, p.BoostWeight, p.Price,
        p.AiToolTier, p.AdPlatformPushAllowed, p.BundledAdCreditKobo, p.IsActive);
}
