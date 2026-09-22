namespace Ogasela.Application.Rbac;

public sealed record OrgUnitResponse(Guid Id, Guid DepartmentId, string Name, string? Description, DateTime CreatedAt);
