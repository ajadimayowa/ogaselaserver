namespace Ogasela.Application.Rbac;

public sealed record DepartmentResponse(Guid Id, string Name, string? Description, DateTime CreatedAt);
