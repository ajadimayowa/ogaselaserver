using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateRole;

/// <summary>RoleType is intentionally not editable here - changing it after creation would let
/// someone route around the create-time hierarchy check (e.g. quietly promote a Standard role to
/// SuperAdmin). Create a new role instead if a different tier is actually needed.</summary>
public sealed record UpdateRoleCommand(Guid Id, string Name, IReadOnlyList<string> PermissionKeys) : IRequest<Result<RoleResponse>>;
