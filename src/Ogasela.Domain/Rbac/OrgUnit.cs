namespace Ogasela.Domain.Rbac;

/// <summary>An organizational unit within a Department (e.g. "Payments" within "Engineering"). Named OrgUnit, not Unit, to avoid colliding with MediatR.Unit wherever both namespaces are imported.</summary>
public class OrgUnit
{
    private OrgUnit()
    {
    }

    public Guid Id { get; private set; }

    public Guid DepartmentId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static OrgUnit Create(Guid departmentId, string name, string? description)
    {
        return new OrgUnit
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}
