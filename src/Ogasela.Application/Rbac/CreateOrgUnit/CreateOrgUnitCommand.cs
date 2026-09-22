using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateOrgUnit;

public sealed record CreateOrgUnitCommand(Guid DepartmentId, string Name, string? Description) : IRequest<Result<OrgUnitResponse>>;
