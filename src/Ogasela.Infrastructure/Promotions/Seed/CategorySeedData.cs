namespace Ogasela.Infrastructure.Promotions.Seed;

/// <summary>
/// Fixed ids/timestamps for the starter category list so the generated EF Core migration is
/// deterministic (HasData snapshots these values at migration-generation time; anything
/// non-deterministic, like Guid.NewGuid() or DateTime.UtcNow, would regenerate a diff on every
/// subsequent migration).
/// </summary>
public static class CategorySeedData
{
    public static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid ElectronicsId = new("c17a0000-0000-0000-0000-000000000001");
    public static readonly Guid FashionId = new("c17a0000-0000-0000-0000-000000000002");
    public static readonly Guid VehiclesId = new("c17a0000-0000-0000-0000-000000000003");
    public static readonly Guid RealEstateId = new("c17a0000-0000-0000-0000-000000000004");
    public static readonly Guid HomeAndFurnitureId = new("c17a0000-0000-0000-0000-000000000005");
    public static readonly Guid JobsId = new("c17a0000-0000-0000-0000-000000000006");
    public static readonly Guid ServicesId = new("c17a0000-0000-0000-0000-000000000007");
}
