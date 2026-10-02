namespace Ogasela.Application.Notifications;

/// <summary>The coarse Category groupings a user's NotificationPreference toggles apply to.</summary>
public static class NotificationCategories
{
    public const string ListingLifecycle = "ListingLifecycle";
    public const string Verification = "Verification";
}

/// <summary>The specific Notification.Type codes NotificationDispatcher raises.</summary>
public static class NotificationTypes
{
    public const string ListingExpiringSoon = "ListingExpiringSoon";
    public const string ListingExpired = "ListingExpired";
    public const string VerificationDecision = "VerificationDecision";
    public const string ListingReviewed = "ListingReviewed";
}
