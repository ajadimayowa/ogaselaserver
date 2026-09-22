using Ogasela.Domain.Notifications;

namespace Ogasela.Application.Notifications.Interfaces;

/// <summary>
/// One delivery channel for notifications. <c>PushNotificationChannel</c> and
/// <c>EmailNotificationChannel</c> (Infrastructure) implement this today; an
/// <c>SmsNotificationChannel</c> could wrap the existing <c>ISmsSender</c>/Termii integration
/// from Phase 1, and a WhatsApp channel would plug in the same way (e.g. via Termii's or
/// Twilio's WhatsApp Business API) - neither is built in this phase.
/// NotificationDispatcher resolves all registered channels via DI and picks the ones the
/// recipient has enabled, so adding a new channel is just registering one more implementation.
/// </summary>
public interface INotificationChannel
{
    NotificationChannel Channel { get; }

    /// <returns>true if delivery succeeded (used to mark the Notification row Sent vs Failed).</returns>
    Task<bool> SendAsync(Guid userId, string type, string payload, CancellationToken cancellationToken);
}
