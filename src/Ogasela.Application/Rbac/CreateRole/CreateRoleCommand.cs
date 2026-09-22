using MediatR;
using Ogasela.Domain.Rbac;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateRole;

public sealed record CreateRoleCommand(string Name, RoleType RoleType, IReadOnlyList<string> PermissionKeys) : IRequest<Result<RoleResponse>>;
