namespace Ogasela.Domain.Notifications;

/// <summary>
/// A single dispatch attempt of one notification to one user over one channel. NotificationDispatcher
/// creates one row per (event, recipient, enabled channel) - two channels enabled means two rows.
/// </summary>
public class Notification
{
    private Notification()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    /// <summary>A short type code, e.g. "ListingExpiringSoon" - see NotificationTypes. Kept as a free string rather than an enum so new types don't require a schema change.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Raw JSON payload describing the event (listing id, expiry date, etc.), for the channel implementation and the client to render from.</summary>
    public string Payload { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Notification CreatePending(Guid userId, NotificationChannel channel, string type, string payload, DateTime now)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            Type = type,
            Payload = payload,
            Status = NotificationStatus.Pending,
            CreatedAt = now
        };
    }

    public void MarkSent() => Status = NotificationStatus.Sent;

    public void MarkFailed() => Status = NotificationStatus.Failed;
}
