using MediatR;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.UpdatePromotionPlan;

public sealed record UpdatePromotionPlanCommand(
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
    bool IsActive) : IRequest<Result<PromotionPlanResponse>>;

/// <summary>Adds a new plan sellers can choose when posting an ad.</summary>
public sealed record CreatePromotionPlanCommand(
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
    bool IsActive) : IRequest<Result<PromotionPlanResponse>>;
