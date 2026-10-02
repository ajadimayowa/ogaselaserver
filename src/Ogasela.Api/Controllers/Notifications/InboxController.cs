using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Notifications.Inbox;

namespace Ogasela.Api.Controllers.Notifications;

/// <summary>The signed-in user's in-app notification inbox (the bell): ad approvals/rejections, expiry, new messages, reviews, verification.</summary>
[ApiController]
[Authorize]
[Route("api/v1/me/inbox")]
public sealed class InboxController : ControllerBase
{
    private readonly ISender _sender;

    public InboxController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetInboxQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), cancellationToken)).ToActionResult(this);

    /// <summary>The bell's badge number.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetUnreadInboxCountQuery(), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new MarkInboxNotificationReadCommand(id), cancellationToken)).ToActionResult(this);

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        (await _sender.Send(new MarkAllInboxNotificationsReadCommand(), cancellationToken)).ToActionResult(this);
}
