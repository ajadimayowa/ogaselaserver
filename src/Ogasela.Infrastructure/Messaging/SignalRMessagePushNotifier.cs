using Microsoft.AspNetCore.SignalR;
using Ogasela.Application.Messaging.Interfaces;
using Ogasela.Application.Messaging.SendMessage;

namespace Ogasela.Infrastructure.Messaging;

public sealed class SignalRMessagePushNotifier : IMessagePushNotifier
{
    private readonly IHubContext<MessagingHub> _hubContext;

    public SignalRMessagePushNotifier(IHubContext<MessagingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushNewMessageAsync(Guid recipientUserId, MessageResponse message, CancellationToken cancellationToken)
    {
        await _hubContext.Clients
            .Group(MessagingHub.GroupNameFor(recipientUserId))
            .SendAsync("ReceiveMessage", message, cancellationToken);
    }
}
