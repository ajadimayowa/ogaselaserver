using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateOrgUnit;

public sealed record UpdateOrgUnitCommand(Guid Id, string Name, string? Description) : IRequest<Result<OrgUnitResponse>>;
