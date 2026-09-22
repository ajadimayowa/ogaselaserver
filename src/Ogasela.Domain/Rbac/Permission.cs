namespace Ogasela.Domain.Rbac;

/// <summary>
/// One row per catalog operation a Role's PermissionKeys can reference (e.g. "staff.create").
/// Seeded at startup by RbacSeeder; Role.PermissionKeys is validated against this catalog when a
/// role is created/updated rather than being FK-enforced (consistent with the rest of the
/// codebase's "bare Guid FK, no navigation" convention).
/// </summary>
public class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static Permission Create(string key, string category, string label)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Key = key,
            Category = category,
            Label = label,
            CreatedAt = DateTime.UtcNow
        };
    }
}
