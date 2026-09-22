namespace Ogasela.Infrastructure.Promotions.Seed;

/// <summary>Fixed ids for the four promotion plan rows - see <see cref="CategorySeedData"/> for why these must be fixed.</summary>
public static class PromotionPlanSeedData
{
    public static readonly Guid FreeId = new("91a70000-0000-0000-0000-000000000001");
    public static readonly Guid BasicId = new("91a70000-0000-0000-0000-000000000002");
    public static readonly Guid StandardId = new("91a70000-0000-0000-0000-000000000003");
    public static readonly Guid PremiumId = new("91a70000-0000-0000-0000-000000000004");
}
