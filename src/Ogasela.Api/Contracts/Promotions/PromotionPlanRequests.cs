using Ogasela.Domain.Promotions;

namespace Ogasela.Api.Contracts.Promotions;

/// <summary>Create and update take the same body; update is a full replace. Price and BundledAdCreditKobo are in kobo.</summary>
public sealed record UpdatePromotionPlanRequest(
    string Name,
    string? Description,
    IReadOnlyList<string>? Features,
    int DurationDays,
    int PhotoLimit,
    bool VideoAllowed,
    int BoostWeight,
    decimal Price,
    AiToolTier AiToolTier,
    bool AdPlatformPushAllowed,
    int BundledAdCreditKobo,
    bool IsActive);
