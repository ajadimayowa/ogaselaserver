using Ogasela.Shared;

namespace Ogasela.Application.Promotions;

public static class PromotionErrors
{
    public static readonly Error CategoryNotFound = new(
        "Category.NotFound", "The category could not be found.");

    public static readonly Error ParentCategoryNotFound = new(
        "Category.ParentNotFound", "The specified parent category could not be found.");

    public static readonly Error CategoryCannotBeOwnParent = new(
        "Category.CannotBeOwnParent", "A category cannot be its own parent.");

    public static readonly Error PromotionPlanNotFound = new(
        "PromotionPlan.NotFound", "The promotion plan could not be found.");
}
