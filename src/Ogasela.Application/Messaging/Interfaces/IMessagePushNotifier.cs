using Ogasela.Application.Messaging.SendMessage;

namespace Ogasela.Application.Messaging.Interfaces;

/// <summary>
/// Pushes a newly sent message to the recipient in real time if they're connected.
/// <c>SignalRMessagePushNotifier</c> (Infrastructure) implements this over the MessagingHub -
/// nothing here assumes SignalR specifically, keeping Application free of transport concerns.
/// Persistence always happens regardless of this call's outcome (store-and-forward): a message
/// with nowhere to push to is still saved for the recipient to fetch later.
/// </summary>
public interface IMessagePushNotifier
{
    Task PushNewMessageAsync(Guid recipientUserId, MessageResponse message, CancellationToken cancellationToken);
}
