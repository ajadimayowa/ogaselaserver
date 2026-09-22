using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Rbac;

namespace Ogasela.Application.Rbac;

/// <summary>
/// The single source of truth for the staff-creation/role-assignment hierarchy: SuperAdmin may
/// assign SuperAdmin or Admin, Admin may only assign HR, HR may only assign Standard roles, and
/// Standard (or non-staff) actors can't create staff or roles at all. Shared by
/// CreateRoleCommandHandler and the Staff module's CreateStaffCommandHandler so the rule is
/// enforced identically in both places.
/// </summary>
public static class StaffHierarchy
{
    /// <summary>
    /// The acting user's tier: the legacy seeded SuperAdmin (UserRole.SuperAdmin) counts as
    /// RoleType.SuperAdmin even though it predates this dynamic-role system; any other non-Staff
    /// account (Buyer/Seller/SupportAgent/Moderator/FinanceAdmin) has no tier and can't create
    /// staff or roles. Returns null if the actor doesn't exist or has no assignable tier.
    /// </summary>
    public static async Task<RoleType?> GetActorTierAsync(IApplicationDbContext dbContext, Guid actorUserId, CancellationToken cancellationToken)
    {
        var actor = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        if (actor.Role == UserRole.SuperAdmin)
        {
            return RoleType.SuperAdmin;
        }

        if (actor.Role != UserRole.Staff)
        {
            return null;
        }

        var staffProfile = await dbContext.StaffProfiles.FirstOrDefaultAsync(s => s.UserId == actorUserId, cancellationToken);
        if (staffProfile is null)
        {
            return null;
        }

        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == staffProfile.RoleId, cancellationToken);
        return role?.RoleType;
    }

    public static bool CanAssign(RoleType actorTier, RoleType targetRoleType) => actorTier switch
    {
        RoleType.SuperAdmin => targetRoleType is RoleType.SuperAdmin or RoleType.Admin,
        RoleType.Admin => targetRoleType == RoleType.HR,
        RoleType.HR => targetRoleType == RoleType.Standard,
        _ => false
    };
}
