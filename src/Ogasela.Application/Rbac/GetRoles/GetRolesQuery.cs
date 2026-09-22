using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetRoles;

public sealed record GetRolesQuery : IRequest<Result<IReadOnlyList<RoleResponse>>>;
