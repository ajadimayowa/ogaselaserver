using Microsoft.AspNetCore.Authorization;

namespace Ogasela.Api.Authorization;

/// <summary>A single required permission key (e.g. "staff.create") from the seeded Permission catalog.</summary>
public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}
