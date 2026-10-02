namespace Ogasela.Domain.Listings;

public enum ListingStatus
{
    Draft,
    Active,
    ExpiringSoon,
    Expired,
    Paused,
    Sold,

    /// <summary>Submitted for publishing and waiting for a moderator to approve it (goes Active) or reject it (back to Draft, with a ReviewNote). Not visible to buyers.</summary>
    PendingReview
}
