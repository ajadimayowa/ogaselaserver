using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Ogasela.Infrastructure.Messaging;

/// <summary>
/// Real-time push channel for messaging. Every authenticated connection joins a group named
/// after its own user id, so <see cref="SignalRMessagePushNotifier"/> can target "whoever is
/// connected as user X" without tracking connection ids itself.
/// </summary>
[Authorize]
public sealed class MessagingHub : Hub
{
    public static string GroupNameFor(Guid userId) => $"user-{userId:N}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameFor(userId));
        }

        await base.OnConnectedAsync();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
