using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Admin.Disputes;
using Ogasela.Domain.Moderation;

namespace Ogasela.Api.Controllers.Admin;

public sealed record StaffDisputeMessageRequest(string Body);

public sealed record ResolveDisputeRequest(DisputeOutcome Outcome, string Note, bool TakeDownListing, Guid? SuspendUserId);

/// <summary>The Control Portal's Disputes &amp; Resolutions page. Viewing needs disputes.view; acting needs disputes.manage.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/disputes")]
public sealed class AdminDisputesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminDisputesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = "perm:disputes.view")]
    public async Task<IActionResult> GetDisputes(
        [FromQuery] DisputeStatus? status, [FromQuery] string? search, [FromQuery] bool assignedToMe,
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new GetAdminDisputesQuery(status, search, assignedToMe, page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize),
            cancellationToken)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "perm:disputes.view")]
    public async Task<IActionResult> GetDispute(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAdminDisputeQuery(id), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/assign-to-me")]
    [Authorize(Policy = "perm:disputes.manage")]
    public async Task<IActionResult> AssignToMe(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new AssignDisputeToMeCommand(id), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/messages")]
    [Authorize(Policy = "perm:disputes.manage")]
    public async Task<IActionResult> AddMessage(Guid id, StaffDisputeMessageRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new AddStaffDisputeMessageCommand(id, request.Body), cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = "perm:disputes.manage")]
    public async Task<IActionResult> Resolve(Guid id, ResolveDisputeRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new ResolveDisputeCommand(id, request.Outcome, request.Note, request.TakeDownListing, request.SuspendUserId),
            cancellationToken)).ToActionResult(this);
}
