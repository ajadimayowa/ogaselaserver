using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Admin.ProfileChanges;
using Ogasela.Domain.Accounts;

namespace Ogasela.Api.Controllers.Admin;

public sealed record ReviewProfileChangeRequest(bool Approve, string? Reason);

/// <summary>
/// App users' phone, email and business-info changes for the Control Portal's Change Requests
/// page. Needs users.changes.review; SuperAdmins pass every check.
/// </summary>
[ApiController]
[Authorize(Policy = "perm:users.changes.review")]
[Route("api/v1/admin/change-requests")]
public sealed class AdminProfileChangesController : ControllerBase
{
    private readonly ISender _sender;

    public AdminProfileChangesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetChangeRequests(
        [FromQuery] ProfileChangeStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new GetAdminProfileChangesQuery(status, page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize),
            cancellationToken)).ToActionResult(this);

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewProfileChangeRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ReviewProfileChangeCommand(id, request.Approve, request.Reason), cancellationToken))
            .ToActionResult(this);
}
