using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetStaffDetail;

public sealed record GetStaffDetailQuery(Guid StaffProfileId) : IRequest<Result<StaffDetailResponse>>;
