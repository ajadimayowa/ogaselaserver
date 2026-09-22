using Ogasela.Domain.Rbac;

namespace Ogasela.Api.Contracts.Rbac;

public sealed record CreateDepartmentRequest(string Name, string? Description);

public sealed record UpdateDepartmentRequest(string Name, string? Description);

public sealed record CreateUnitRequest(Guid DepartmentId, string Name, string? Description);

public sealed record UpdateUnitRequest(string Name, string? Description);

public sealed record CreateRoleRequest(string Name, RoleType RoleType, IReadOnlyList<string> PermissionKeys);

public sealed record UpdateRoleRequest(string Name, IReadOnlyList<string> PermissionKeys);
