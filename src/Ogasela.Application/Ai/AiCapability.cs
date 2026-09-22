namespace Ogasela.Application.Ai;

/// <summary>
/// The seller-facing AI tools gated by PromotionPlan.AiToolTier. AdCopyAssist is reserved for
/// Phase 10 (the ad-copy assist) - listed now so AiAccessGuard's tier table doesn't need to
/// change shape when that lands, just gain a new case.
/// </summary>
public enum AiCapability
{
    DescriptionGeneration,
    PriceSuggestion,
    ImageEnhancement,
    AdCopyAssist
}
