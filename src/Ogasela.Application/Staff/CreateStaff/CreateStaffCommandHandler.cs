using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Rbac;
using Ogasela.Application.Staff.GetStaffDetail;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.CreateStaff;

/// <summary>
/// Hierarchy-gated the same way as CreateRoleCommandHandler: which RoleType the actor may assign
/// to a new staff member is decided entirely by StaffHierarchy, not by the endpoint's
/// "perm:staff.create" policy (that's only a first-pass "are you even allowed to onboard staff at
/// all" gate). Creates the User in a PendingApproval, no-usable-password state - a real password
/// is only generated and emailed once ApproveStaffOnboardingCommand approves the profile.
/// </summary>
public sealed class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, Result<StaffDetailResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _auditLogger;

    public CreateStaffCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _auditLogger = auditLogger;
    }

    public async Task<Result<StaffDetailResponse>> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.RoleNotFound);
        }

        var actorTier = await StaffHierarchy.GetActorTierAsync(_dbContext, _currentUser.UserId!.Value, cancellationToken);
        if (actorTier is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.ActorNotEligible);
        }

        if (!StaffHierarchy.CanAssign(actorTier.Value, role.RoleType))
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.InsufficientHierarchyPermission);
        }

        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.DepartmentNotFound);
        }

        var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.UnitNotFound);
        }

        var alreadyExists = await _dbContext.Users.AnyAsync(
            u => (request.Phone != null && u.Phone == request.Phone) ||
                 (request.Email != null && u.Email == request.Email),
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.EmailOrPhoneAlreadyExists);
        }

        // No usable password yet - nothing can ever hash to match this, so the account can't log
        // in until ApproveStaffOnboardingCommand generates and sets a real one.
        var unusableHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        var user = User.Create(request.Phone, request.Email, unusableHash, UserRole.Staff, request.Name);
        _dbContext.Users.Add(user);

        var staffProfile = StaffProfile.Create(user.Id, request.DepartmentId, request.UnitId, request.RoleId, _currentUser.UserId.Value);
        _dbContext.StaffProfiles.Add(staffProfile);

        await _auditLogger.LogAsync(
            _currentUser.UserId.Value,
            "Staff.Create",
            nameof(StaffProfile),
            staffProfile.Id,
            beforeJson: null,
            afterJson: JsonSerializer.Serialize(new { user.Name, user.Email, user.Phone, request.DepartmentId, request.UnitId, request.RoleId }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new StaffDetailResponse(
            staffProfile.Id,
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            department.Name,
            unit.Name,
            role.Name,
            staffProfile.Status,
            staffProfile.CreatedAt,
            staffProfile.RejectionReason,
            staffProfile.DecidedAt,
            Documents: []);

        return Result.Success(response);
    }
}
