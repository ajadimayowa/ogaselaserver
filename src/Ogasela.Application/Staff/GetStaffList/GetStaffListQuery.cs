using MediatR;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetStaffList;

public sealed record GetStaffListQuery(StaffOnboardingStatus? Status) : IRequest<Result<IReadOnlyList<StaffListItemResponse>>>;
