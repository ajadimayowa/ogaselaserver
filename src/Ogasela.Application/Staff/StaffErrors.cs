using Ogasela.Shared;

namespace Ogasela.Application.Staff;

public static class StaffErrors
{
    public static readonly Error StaffProfileNotFound = new(
        "Staff.NotFound", "The staff profile could not be found.");

    public static readonly Error UserNotFound = new(
        "Staff.UserNotFound", "The user for this staff profile could not be found.");

    public static readonly Error DepartmentNotFound = new(
        "Staff.DepartmentNotFound", "The department could not be found.");

    public static readonly Error UnitNotFound = new(
        "Staff.UnitNotFound", "The unit could not be found.");

    public static readonly Error RoleNotFound = new(
        "Staff.RoleNotFound", "The role could not be found.");

    public static readonly Error AlreadyDecided = new(
        "Staff.AlreadyDecided", "This staff profile has already been approved or rejected.");

    public static readonly Error EmailOrPhoneAlreadyExists = new(
        "Staff.EmailOrPhoneAlreadyExists", "A user with this phone number or email already exists.");

    public static readonly Error InsufficientHierarchyPermission = new(
        "Staff.InsufficientHierarchyPermission", "Your role cannot create or act on a role of this type.");

    public static readonly Error ActorNotEligible = new(
        "Staff.ActorNotEligible", "Your account is not set up to manage staff.");
}
