using MediatR;
using Ogasela.Application.Staff.GetStaffDetail;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.CreateStaff;

public sealed record CreateStaffCommand(
    string Name,
    string? Phone,
    string? Email,
    Guid DepartmentId,
    Guid UnitId,
    Guid RoleId) : IRequest<Result<StaffDetailResponse>>;
