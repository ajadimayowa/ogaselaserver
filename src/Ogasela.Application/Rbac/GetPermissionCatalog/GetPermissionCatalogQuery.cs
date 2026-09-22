using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetPermissionCatalog;

public sealed record GetPermissionCatalogQuery : IRequest<Result<IReadOnlyList<PermissionResponse>>>;
