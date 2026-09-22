using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Notifications;

namespace Ogasela.IntegrationTests.Reviews;

/// <summary>Records what it was asked to send and always "succeeds", so tests never touch real FCM/SMTP.</summary>
public sealed class FakeNotificationChannel : INotificationChannel
{
    public FakeNotificationChannel(NotificationChannel channel)
    {
        Channel = channel;
    }

    public NotificationChannel Channel { get; }

    public List<(Guid UserId, string Type, string Payload)> SentMessages { get; } = [];

    public Task<bool> SendAsync(Guid userId, string type, string payload, CancellationToken cancellationToken)
    {
        SentMessages.Add((userId, type, payload));
        return Task.FromResult(true);
    }
}
