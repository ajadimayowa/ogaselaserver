using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Messaging;
using Ogasela.Application.Messaging.ReportUser;
using Ogasela.Application.Moderation.ReportListing;

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

    /// <summary>Reports an ad for review (Report.TargetType.Listing, Status Open) into the same Moderator queue. Can't report your own ad; re-reporting returns your existing open report.</summary>
    [HttpPost("api/v1/listings/{id:guid}/report")]
    public async Task<IActionResult> ReportListing(Guid id, ReportListingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReportListingCommand(id, request.Reason), cancellationToken);
        return result.ToActionResult(this);
    }
}
