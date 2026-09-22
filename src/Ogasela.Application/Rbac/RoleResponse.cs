using Ogasela.Domain.Rbac;

namespace Ogasela.Application.Rbac;

public sealed record RoleResponse(Guid Id, string Name, RoleType RoleType, IReadOnlyList<string> PermissionKeys, DateTime CreatedAt);
