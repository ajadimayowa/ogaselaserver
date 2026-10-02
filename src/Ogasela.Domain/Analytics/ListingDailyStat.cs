namespace Ogasela.Domain.Analytics;

/// <summary>
/// One row per listing per day of on-platform engagement. Counters only ever go up, written by
/// atomic database upserts (see Infrastructure's EngagementTracker) rather than through this
/// entity, so concurrent requests never lose increments. SellerId is denormalised so a seller's
/// totals don't need a join to Listings.
/// </summary>
public class ListingDailyStat
{
    private ListingDailyStat()
    {
    }

    public Guid ListingId { get; private set; }

    public Guid SellerId { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>Times the listing appeared in search results or a home-screen rail.</summary>
    public long Impressions { get; private set; }

    /// <summary>Times someone other than the seller opened the listing's detail page.</summary>
    public long Views { get; private set; }

    /// <summary>Taps on the Call button.</summary>
    public long CallClicks { get; private set; }

    /// <summary>New buyer conversations started from the listing.</summary>
    public long MessageStarts { get; private set; }

    /// <summary>Times the listing was saved to someone's favourites.</summary>
    public long Saves { get; private set; }

    /// <summary>For tests and seeding - production writes go through the tracker's upserts.</summary>
    public static ListingDailyStat Create(
        Guid listingId, Guid sellerId, DateOnly date, long impressions, long views, long callClicks, long messageStarts, long saves) => new()
    {
        ListingId = listingId,
        SellerId = sellerId,
        Date = date,
        Impressions = impressions,
        Views = views,
        CallClicks = callClicks,
        MessageStarts = messageStarts,
        Saves = saves
    };
}
