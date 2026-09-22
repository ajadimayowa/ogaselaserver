using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Messaging;
using Ogasela.Application.Messaging.ReportUser;

namespace Ogasela.Api.Controllers.Moderation;

[ApiController]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Reports a user for review (Report.TargetType.User, Status Open). Appears in the Moderator moderation queue.</summary>
    [HttpPost("api/v1/reports")]
    public async Task<IActionResult> Report(ReportUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReportUserCommand(request.TargetUserId, request.Reason), cancellationToken);
        return result.ToActionResult(this);
    }
}
