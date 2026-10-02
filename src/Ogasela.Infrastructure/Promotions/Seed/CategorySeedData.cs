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

    public static readonly Guid PhonesAndTabletsId = new("c17a0001-0000-0000-0000-000000001001");
    public static readonly Guid WomensClothingId = new("c17a0001-0000-0000-0000-000000002001");
    public static readonly Guid CarsId = new("c17a0001-0000-0000-0000-000000003001");

    /// <summary>Starter subcategories for each seeded top-level category, as (Id, ParentId, Name). Ids are fixed for the same HasData reason as above: c17a0001-...-00000000PPCC, where PP is the parent's number and CC the child's.</summary>
    public static readonly (Guid Id, Guid ParentId, string Name)[] Subcategories =
    [
        (PhonesAndTabletsId, ElectronicsId, "Phones & Tablets"),
        (new Guid("c17a0001-0000-0000-0000-000000001002"), ElectronicsId, "Laptops & Computers"),
        (new Guid("c17a0001-0000-0000-0000-000000001003"), ElectronicsId, "TVs & Audio"),
        (new Guid("c17a0001-0000-0000-0000-000000001004"), ElectronicsId, "Gaming"),
        (new Guid("c17a0001-0000-0000-0000-000000001005"), ElectronicsId, "Cameras"),
        (new Guid("c17a0001-0000-0000-0000-000000001006"), ElectronicsId, "Accessories"),
        (WomensClothingId, FashionId, "Women's Clothing"),
        (new Guid("c17a0001-0000-0000-0000-000000002002"), FashionId, "Men's Clothing"),
        (new Guid("c17a0001-0000-0000-0000-000000002003"), FashionId, "Shoes"),
        (new Guid("c17a0001-0000-0000-0000-000000002004"), FashionId, "Bags"),
        (new Guid("c17a0001-0000-0000-0000-000000002005"), FashionId, "Watches & Jewellery"),
        (CarsId, VehiclesId, "Cars"),
        (new Guid("c17a0001-0000-0000-0000-000000003002"), VehiclesId, "Motorcycles"),
        (new Guid("c17a0001-0000-0000-0000-000000003003"), VehiclesId, "Trucks & Buses"),
        (new Guid("c17a0001-0000-0000-0000-000000003004"), VehiclesId, "Vehicle Parts"),
        (new Guid("c17a0001-0000-0000-0000-000000004001"), RealEstateId, "Houses & Apartments for Rent"),
        (new Guid("c17a0001-0000-0000-0000-000000004002"), RealEstateId, "Houses & Apartments for Sale"),
        (new Guid("c17a0001-0000-0000-0000-000000004003"), RealEstateId, "Land & Plots"),
        (new Guid("c17a0001-0000-0000-0000-000000004004"), RealEstateId, "Commercial Property"),
        (new Guid("c17a0001-0000-0000-0000-000000005001"), HomeAndFurnitureId, "Furniture"),
        (new Guid("c17a0001-0000-0000-0000-000000005002"), HomeAndFurnitureId, "Home Appliances"),
        (new Guid("c17a0001-0000-0000-0000-000000005003"), HomeAndFurnitureId, "Kitchen & Dining"),
        (new Guid("c17a0001-0000-0000-0000-000000005004"), HomeAndFurnitureId, "Bedding & Decor"),
        (new Guid("c17a0001-0000-0000-0000-000000006001"), JobsId, "Full-time"),
        (new Guid("c17a0001-0000-0000-0000-000000006002"), JobsId, "Part-time"),
        (new Guid("c17a0001-0000-0000-0000-000000006003"), JobsId, "Internships"),
        (new Guid("c17a0001-0000-0000-0000-000000007001"), ServicesId, "Repairs"),
        (new Guid("c17a0001-0000-0000-0000-000000007002"), ServicesId, "Beauty & Wellness"),
        (new Guid("c17a0001-0000-0000-0000-000000007003"), ServicesId, "Cleaning"),
        (new Guid("c17a0001-0000-0000-0000-000000007004"), ServicesId, "Events & Catering"),
        (new Guid("c17a0001-0000-0000-0000-000000007005"), ServicesId, "Tutoring & Lessons"),
    ];
}
