using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Ogasela.Api.Authorization;

/// <summary>
/// Succeeds if the user is a SuperAdmin (legacy ClaimTypes.Role="SuperAdmin", from the
/// pre-existing seeded account, or the new role_type="SuperAdmin" claim from a dynamic Staff
/// role - either flavour can do anything a narrower permission would allow, matching the
/// convention already established on AdminController), or if the required "permission" claim is
/// present.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var isSuperAdmin =
            context.User.HasClaim(ClaimTypes.Role, "SuperAdmin") ||
            context.User.HasClaim("role_type", "SuperAdmin");

        if (isSuperAdmin || context.User.HasClaim("permission", requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
