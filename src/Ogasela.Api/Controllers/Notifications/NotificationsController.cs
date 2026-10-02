using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Notifications;
using Ogasela.Application.Notifications.GetNotifications;
using Ogasela.Application.Notifications.GetPreferences;
using Ogasela.Application.Notifications.UpdatePreferences;

namespace Ogasela.Api.Controllers.Notifications;

[ApiController]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>The caller's own in-app notification history, newest first, across every channel (push/email/in-app).</summary>
    [HttpGet("api/v1/notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new GetNotificationsQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The caller's toggles for every (Channel, Category) pair the app offers; untouched pairs are enabled.</summary>
    [HttpGet("api/v1/notifications/preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetNotificationPreferencesQuery(), cancellationToken)).ToActionResult(this);

    /// <summary>Enables/disables specific (Channel, Category) combinations - e.g. turn off Email for ListingLifecycle while leaving Push on. Only the pairs included in the request are changed.</summary>
    [HttpPut("api/v1/notifications/preferences")]
    public async Task<IActionResult> UpdatePreferences(
        UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        var preferences = request.Preferences
            .Select(p => new PreferenceUpdate(p.Channel, p.Category, p.Enabled))
            .ToList();

        var result = await _sender.Send(new UpdateNotificationPreferencesCommand(preferences), cancellationToken);
        return result.ToActionResult(this);
    }
}
