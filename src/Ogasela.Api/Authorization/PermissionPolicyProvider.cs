using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ogasela.Api.Authorization;

/// <summary>
/// Recognizes policy names prefixed "perm:" (e.g. "perm:staff.create") and builds a
/// PermissionRequirement for them on the fly, since permission keys are dynamic strings from the
/// database rather than a fixed set that could be pre-registered. Every other policy name (the
/// existing per-UserRole policies registered in Program.cs, e.g. "SuperAdmin") falls through to
/// the default provider unchanged.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "perm:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var permissionKey = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionKey))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
