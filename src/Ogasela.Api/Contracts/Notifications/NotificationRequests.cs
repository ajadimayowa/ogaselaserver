using Ogasela.Domain.Notifications;

namespace Ogasela.Api.Contracts.Notifications;

public sealed record UpdateNotificationPreferencesRequest(IReadOnlyList<PreferenceUpdateRequest> Preferences);

public sealed record PreferenceUpdateRequest(NotificationChannel Channel, string Category, bool Enabled);
