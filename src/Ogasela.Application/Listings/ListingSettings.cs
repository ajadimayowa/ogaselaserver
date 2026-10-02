namespace Ogasela.Application.Listings;

public sealed class ListingSettings
{
    public const string SectionName = "Listings";

    /// <summary>Max number of concurrently Active Free-plan listings a seller may have (RULE-05).</summary>
    public int FreePlanCap { get; init; } = 3;

    /// <summary>
    /// When true (the default), a publish that passes every rule puts the listing in PendingReview
    /// for a moderator to approve, instead of making it Active straight away.
    /// </summary>
    public bool RequireApproval { get; init; } = true;
}
