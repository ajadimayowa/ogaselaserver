using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Rbac;
using Ogasela.Application.Rbac.CreateDepartment;
using Ogasela.Application.Rbac.CreateOrgUnit;
using Ogasela.Application.Rbac.CreateRole;
using Ogasela.Application.Rbac.GetDepartments;
using Ogasela.Application.Rbac.GetPermissionCatalog;
using Ogasela.Application.Rbac.GetRoles;
using Ogasela.Application.Rbac.GetUnits;
using Ogasela.Application.Rbac.UpdateDepartment;
using Ogasela.Application.Rbac.UpdateOrgUnit;
using Ogasela.Application.Rbac.UpdateRole;

namespace Ogasela.Api.Controllers.Rbac;

/// <summary>
/// Departments/units are permission-gated the same for anyone with "rbac.department.manage"/
/// "rbac.unit.manage" (a flat management right - no hierarchy involved). Roles are different:
/// "perm:rbac.role.manage" is only the first-pass "can you touch role management at all" gate -
/// which specific RoleType you can create/edit is then re-checked per request inside
/// CreateRoleCommandHandler/UpdateRoleCommandHandler via StaffHierarchy, since that depends on
/// the caller's own tier and the target role, not something a static policy can express.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/rbac")]
public sealed class RbacController : ControllerBase
{
    private readonly ISender _sender;

    public RbacController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("departments")]
    [Authorize(Policy = "perm:rbac.department.manage")]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("departments")]
    [Authorize(Policy = "perm:rbac.department.manage")]
    public async Task<IActionResult> CreateDepartment(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateDepartmentCommand(request.Name, request.Description), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("departments/{id:guid}")]
    [Authorize(Policy = "perm:rbac.department.manage")]
    public async Task<IActionResult> UpdateDepartment(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateDepartmentCommand(id, request.Name, request.Description), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("units")]
    [Authorize(Policy = "perm:rbac.unit.manage")]
    public async Task<IActionResult> GetUnits([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUnitsQuery(departmentId), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("units")]
    [Authorize(Policy = "perm:rbac.unit.manage")]
    public async Task<IActionResult> CreateUnit(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateOrgUnitCommand(request.DepartmentId, request.Name, request.Description), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("units/{id:guid}")]
    [Authorize(Policy = "perm:rbac.unit.manage")]
    public async Task<IActionResult> UpdateUnit(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateOrgUnitCommand(id, request.Name, request.Description), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The full seeded catalog of assignable operations, for the Role create/edit UI's permission checkboxes.</summary>
    [HttpGet("permissions")]
    [Authorize(Policy = "perm:rbac.role.manage")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPermissionCatalogQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("roles")]
    [Authorize(Policy = "perm:rbac.role.manage")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRolesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("roles")]
    [Authorize(Policy = "perm:rbac.role.manage")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateRoleCommand(request.Name, request.RoleType, request.PermissionKeys), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("roles/{id:guid}")]
    [Authorize(Policy = "perm:rbac.role.manage")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateRoleCommand(id, request.Name, request.PermissionKeys), cancellationToken);
        return result.ToActionResult(this);
    }
}
