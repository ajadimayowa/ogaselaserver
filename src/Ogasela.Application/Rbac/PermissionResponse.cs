namespace Ogasela.Application.Rbac;

public sealed record PermissionResponse(Guid Id, string Key, string Category, string Label);
