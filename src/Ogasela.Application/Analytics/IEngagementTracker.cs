namespace Ogasela.Application.Analytics;

public enum ListingEngagement
{
    Impression,
    View,
    CallClick,
    MessageStart,
    Save
}

/// <summary>
/// Records on-platform engagement into today's daily counters. Best-effort by contract: an
/// implementation must never throw - analytics failing can't be allowed to break the search,
/// listing page or chat that triggered it.
/// </summary>
public interface IEngagementTracker
{
    Task RecordAsync(IReadOnlyCollection<(Guid ListingId, Guid SellerId)> listings, ListingEngagement engagement, CancellationToken cancellationToken);

    Task RecordProfileVisitAsync(Guid sellerId, CancellationToken cancellationToken);
}
