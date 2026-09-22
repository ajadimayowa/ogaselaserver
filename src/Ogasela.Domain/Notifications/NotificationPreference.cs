namespace Ogasela.Domain.Notifications;

/// <summary>
/// Whether a user wants a given notification Category delivered over a given Channel. The
/// literal spec field list has no Id, but a synthetic key (with a unique index on
/// UserId+Channel+Category) is used here for consistency with every other entity in the app -
/// the (UserId, Channel, Category) triple is still what's actually unique.
/// Absence of a row for a (user, channel, category) combination means "enabled" (opt-out
/// model) - only an explicit row with Enabled = false suppresses that combination.
/// </summary>
public class NotificationPreference
{
    private NotificationPreference()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    /// <summary>A coarse grouping of notification Types, e.g. "ListingLifecycle" - see NotificationCategories.</summary>
    public string Category { get; private set; } = string.Empty;

    public bool Enabled { get; private set; }

    public static NotificationPreference Create(Guid userId, NotificationChannel channel, string category, bool enabled)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            Category = category,
            Enabled = enabled
        };
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;
}
