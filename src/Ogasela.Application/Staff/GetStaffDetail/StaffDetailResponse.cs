using Ogasela.Domain.Staff;

namespace Ogasela.Application.Staff.GetStaffDetail;

public sealed record StaffDetailResponse(
    Guid Id,
    Guid UserId,
    string? Name,
    string? Email,
    string? Phone,
    string DepartmentName,
    string UnitName,
    string RoleName,
    StaffOnboardingStatus Status,
    DateTime CreatedAt,
    string? RejectionReason,
    DateTime? DecidedAt,
    IReadOnlyList<StaffKycDocumentResponse> Documents);
