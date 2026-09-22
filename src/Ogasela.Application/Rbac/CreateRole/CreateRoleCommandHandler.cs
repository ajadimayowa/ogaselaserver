using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Rbac;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateRole;

/// <summary>
/// Hierarchy-gated: which RoleType the caller may assign is decided by StaffHierarchy, not by an
/// [Authorize] policy - see StaffHierarchy's doc comment for the exact chain (SuperAdmin may
/// create SuperAdmin/Admin roles, Admin may only create HR roles, HR may only create Standard
/// roles). The endpoint-level "perm:rbac.role.manage" policy is only a first-pass "are you even
/// staff with role-management rights" gate.
/// </summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IAuditLogger _auditLogger;

    public CreateRoleCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLogger = auditLogger;
    }

    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var actorTier = await StaffHierarchy.GetActorTierAsync(_dbContext, _currentUser.UserId!.Value, cancellationToken);
        if (actorTier is null)
        {
            return Result.Failure<RoleResponse>(RbacErrors.ActorNotEligible);
        }

        if (!StaffHierarchy.CanAssign(actorTier.Value, request.RoleType))
        {
            return Result.Failure<RoleResponse>(RbacErrors.InsufficientHierarchyPermission);
        }

        var nameTaken = await _dbContext.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<RoleResponse>(RbacErrors.RoleNameTaken);
        }

        var validKeys = await _dbContext.Permissions.Select(p => p.Key).ToListAsync(cancellationToken);
        if (request.PermissionKeys.Except(validKeys).Any())
        {
            return Result.Failure<RoleResponse>(RbacErrors.InvalidPermissionKeys);
        }

        var role = Role.Create(request.Name, request.RoleType, request.PermissionKeys);
        _dbContext.Roles.Add(role);

        await _auditLogger.LogAsync(
            _currentUser.UserId.Value,
            "Role.Create",
            nameof(Role),
            role.Id,
            beforeJson: null,
            afterJson: JsonSerializer.Serialize(new { role.Name, RoleType = role.RoleType.ToString(), role.PermissionKeys }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RoleResponse(role.Id, role.Name, role.RoleType, role.PermissionKeys, role.CreatedAt));
    }
}
