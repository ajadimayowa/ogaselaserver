using Ogasela.Domain.Staff;

namespace Ogasela.Application.Staff.GetStaffList;

public sealed record StaffListItemResponse(
    Guid Id,
    Guid UserId,
    string? Name,
    string? Email,
    string? Phone,
    string DepartmentName,
    string UnitName,
    string RoleName,
    StaffOnboardingStatus Status,
    DateTime CreatedAt);
