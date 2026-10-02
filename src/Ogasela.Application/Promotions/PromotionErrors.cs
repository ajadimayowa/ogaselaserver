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

    public static readonly Error ParentMustBeTopLevel = new(
        "Category.ParentMustBeTopLevel", "Subcategories can only be added to a top-level category - categories are two levels deep.");

    public static readonly Error CategoryHasSubcategories = new(
        "Category.HasSubcategories", "This category has subcategories, so it can't itself become a subcategory.");

    public static readonly Error PromotionPlanNotFound = new(
        "PromotionPlan.NotFound", "The promotion plan could not be found.");

    public static readonly Error PromotionPlanNameTaken = new(
        "PromotionPlan.NameTaken", "There's already a plan with that name.");

    public static readonly Error FreePlanAlreadyExists = new(
        "PromotionPlan.FreePlanExists", "There's already a free (₦0) plan. Give this plan a price, or edit the existing free plan.");
}
