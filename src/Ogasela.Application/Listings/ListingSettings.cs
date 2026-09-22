namespace Ogasela.Application.Listings;

public sealed class ListingSettings
{
    public const string SectionName = "Listings";

    /// <summary>Max number of concurrently Active Free-plan listings a seller may have (RULE-05).</summary>
    public int FreePlanCap { get; init; } = 3;
}
