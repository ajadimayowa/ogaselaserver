using Ogasela.Shared;

namespace Ogasela.Application.Rbac;

public static class RbacErrors
{
    public static readonly Error DepartmentNotFound = new(
        "Rbac.DepartmentNotFound", "The department could not be found.");

    public static readonly Error DepartmentNameTaken = new(
        "Rbac.DepartmentNameTaken", "A department with this name already exists.");

    public static readonly Error UnitNotFound = new(
        "Rbac.UnitNotFound", "The unit could not be found.");

    public static readonly Error UnitNameTaken = new(
        "Rbac.UnitNameTaken", "A unit with this name already exists in this department.");

    public static readonly Error RoleNotFound = new(
        "Rbac.RoleNotFound", "The role could not be found.");

    public static readonly Error RoleNameTaken = new(
        "Rbac.RoleNameTaken", "A role with this name already exists.");

    public static readonly Error InvalidPermissionKeys = new(
        "Rbac.InvalidPermissionKeys", "One or more permission keys are not in the permission catalog.");

    public static readonly Error InsufficientHierarchyPermission = new(
        "Rbac.InsufficientHierarchyPermission", "Your role cannot create or assign a role of this type.");

    public static readonly Error ActorNotEligible = new(
        "Rbac.ActorNotEligible", "Your account is not set up to manage roles.");
}
