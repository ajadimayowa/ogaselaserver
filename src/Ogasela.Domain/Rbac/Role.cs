namespace Ogasela.Domain.Rbac;

/// <summary>
/// A named, permission-bearing role a StaffProfile is assigned. RoleType is what drives the
/// staff-creation hierarchy (see CreateStaffCommandHandler); Name is just a display label ("HR",
/// "Content Moderator", ...) and can repeat across RoleTypes.
/// </summary>
public class Role
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public RoleType RoleType { get; private set; }

    /// <summary>Mapped to a native Postgres text[] column. The list reference itself can't be
    /// reassigned from outside (private setter), but callers should treat it as read-only and go
    /// through <see cref="Update"/> to change it.</summary>
    public List<string> PermissionKeys { get; private set; } = [];

    public DateTime CreatedAt { get; private set; }

    public static Role Create(string name, RoleType roleType, IEnumerable<string> permissionKeys)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            RoleType = roleType,
            PermissionKeys = permissionKeys.Distinct().ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, IEnumerable<string> permissionKeys)
    {
        Name = name;
        PermissionKeys = permissionKeys.Distinct().ToList();
    }
}
