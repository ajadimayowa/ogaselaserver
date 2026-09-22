using MediatR;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Notifications.UpdatePreferences;

public sealed record UpdateNotificationPreferencesCommand(IReadOnlyList<PreferenceUpdate> Preferences) : IRequest<Result>;

public sealed record PreferenceUpdate(NotificationChannel Channel, string Category, bool Enabled);
