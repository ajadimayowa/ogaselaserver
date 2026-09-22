using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IApplicationDbContext _dbContext;

    public JwtTokenService(IOptions<JwtSettings> options, IApplicationDbContext dbContext)
    {
        _settings = options.Value;
        _dbContext = dbContext;
    }

    public async Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim("phone_number", user.Phone));
        }

        if (user.Role == UserRole.Staff)
        {
            await AddStaffClaimsAsync(claims, user.Id, cancellationToken);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (rawToken, expiresAt);
    }

    /// <summary>
    /// Embeds one "permission" claim per key on the staff member's Role, plus "role_type",
    /// "department_id" and "unit_id" - PermissionAuthorizationHandler reads these to authorize
    /// the new RBAC/Staff/Geo endpoints without a database round trip per request. If the
    /// StaffProfile/Role can't be found (data inconsistency), the token is still issued but with
    /// no permission claims - every "perm:" policy then simply denies, which is the safe failure
    /// mode.
    /// </summary>
    private async Task AddStaffClaimsAsync(List<Claim> claims, Guid userId, CancellationToken cancellationToken)
    {
        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (staffProfile is null)
        {
            return;
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == staffProfile.RoleId, cancellationToken);
        if (role is null)
        {
            return;
        }

        claims.Add(new Claim("role_type", role.RoleType.ToString()));
        claims.Add(new Claim("department_id", staffProfile.DepartmentId.ToString()));
        claims.Add(new Claim("unit_id", staffProfile.UnitId.ToString()));

        foreach (var permissionKey in role.PermissionKeys)
        {
            claims.Add(new Claim("permission", permissionKey));
        }
    }

    public string GenerateRefreshTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
