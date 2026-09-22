using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Rbac;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateRole;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public UpdateRoleCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleResponse>(RbacErrors.RoleNotFound);
        }

        var actorTier = await StaffHierarchy.GetActorTierAsync(_dbContext, _currentUser.UserId!.Value, cancellationToken);
        if (actorTier is null || !StaffHierarchy.CanAssign(actorTier.Value, role.RoleType))
        {
            return Result.Failure<RoleResponse>(RbacErrors.InsufficientHierarchyPermission);
        }

        var nameTaken = await _dbContext.Roles.AnyAsync(r => r.Id != request.Id && r.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<RoleResponse>(RbacErrors.RoleNameTaken);
        }

        var validKeys = await _dbContext.Permissions.Select(p => p.Key).ToListAsync(cancellationToken);
        if (request.PermissionKeys.Except(validKeys).Any())
        {
            return Result.Failure<RoleResponse>(RbacErrors.InvalidPermissionKeys);
        }

        var beforeJson = JsonSerializer.Serialize(new { role.Name, role.PermissionKeys });

        role.Update(request.Name, request.PermissionKeys);

        await _auditLogger.LogAsync(
            _currentUser.UserId.Value,
            "Role.Update",
            nameof(Role),
            role.Id,
            beforeJson,
            JsonSerializer.Serialize(new { role.Name, role.PermissionKeys }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RoleResponse(role.Id, role.Name, role.RoleType, role.PermissionKeys, role.CreatedAt));
    }
}
