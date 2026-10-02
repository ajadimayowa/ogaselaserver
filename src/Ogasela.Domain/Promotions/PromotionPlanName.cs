namespace Ogasela.Domain.Promotions;

/// <summary>
/// The names of the four plans Ogasela launched with. Plan names are free text - SuperAdmins add and
/// rename plans from the Control Portal - so these only identify the seeded plans (seed data, tests).
/// Never use them to decide behaviour: a plan is free when its price is 0 (<see cref="PromotionPlan.IsFree"/>).
/// </summary>
public static class PromotionPlanName
{
    public const string Free = "Free";
    public const string Basic = "Basic";
    public const string Standard = "Standard";
    public const string Premium = "Premium";
}
