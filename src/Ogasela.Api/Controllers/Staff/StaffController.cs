using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Staff;
using Ogasela.Application.Staff.ApproveStaffOnboarding;
using Ogasela.Application.Staff.CreateStaff;
using Ogasela.Application.Staff.GetStaffDetail;
using Ogasela.Application.Staff.GetStaffList;
using Ogasela.Application.Staff.RejectStaffOnboarding;
using Ogasela.Application.Staff.UploadStaffKycDocument;
using Ogasela.Domain.Staff;

namespace Ogasela.Api.Controllers.Staff;

/// <summary>
/// "perm:staff.create"/"perm:staff.view"/"perm:staff.kyc.approve"/"perm:staff.kyc.reject" are
/// only the first-pass "can you touch staff onboarding at all" gates - which specific RoleType a
/// caller can create or decide on is re-checked per request inside the command handlers via
/// StaffHierarchy, since that depends on the caller's own tier and the target role (see
/// RbacController's doc comment for the same pattern applied to role management).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly ISender _sender;

    public StaffController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = "perm:staff.create")]
    public async Task<IActionResult> CreateStaff(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateStaffCommand(request.Name, request.Phone, request.Email, request.DepartmentId, request.UnitId, request.RoleId);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    [Authorize(Policy = "perm:staff.view")]
    public async Task<IActionResult> GetStaffList([FromQuery] StaffOnboardingStatus? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStaffListQuery(status), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "perm:staff.view")]
    public async Task<IActionResult> GetStaffDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStaffDetailQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Uploads a single KYC document (multipart/form-data, up to 10MB) - part of onboarding, gated the same as create.</summary>
    [HttpPost("{id:guid}/documents")]
    [Authorize(Policy = "perm:staff.create")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] UploadStaffKycDocumentRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new UploadStaffKycDocumentCommand(id, request.DocumentType, stream, request.File.FileName, request.File.ContentType);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "perm:staff.kyc.approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ApproveStaffOnboardingCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "perm:staff.kyc.reject")]
    public async Task<IActionResult> Reject(Guid id, RejectStaffOnboardingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectStaffOnboardingCommand(id, request.Reason), cancellationToken);
        return result.ToActionResult(this);
    }
}
