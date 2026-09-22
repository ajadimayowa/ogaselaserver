using Ogasela.Shared;

namespace Ogasela.Application.Ai;

public static class AiErrors
{
    public static readonly Error PlanNotFound = new(
        "Ai.PlanNotFound", "The promotion plan could not be found.");

    public static readonly Error NotIncludedInPlan = new(
        "Ai.NotIncludedInPlan", "This AI tool is not included in your current plan.");

    public static readonly Error GenerationFailed = new(
        "Ai.GenerationFailed", "The AI provider could not generate a suggestion right now. Try again shortly.");

    public static readonly Error NotEnoughComparableData = new(
        "Ai.NotEnoughComparableData", "There aren't enough comparable listings yet to suggest a price for this category.");

    public static readonly Error EnhancementFailed = new(
        "Ai.EnhancementFailed", "The image could not be enhanced.");
}
