namespace Ogasela.Domain.Rbac;

/// <summary>
/// Encodes the staff-creation hierarchy, independent of a Role's free-text Name: SuperAdmin can
/// create SuperAdmin/Admin roles, Admin can only create HR roles, HR can create any Standard
/// role, and Standard roles can't create staff at all. See CreateStaffCommandHandler for the
/// actual enforcement.
/// </summary>
public enum RoleType
{
    SuperAdmin,
    Admin,
    HR,
    Standard
}
