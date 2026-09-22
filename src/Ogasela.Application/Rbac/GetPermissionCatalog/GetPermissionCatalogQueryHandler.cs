using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler : IRequestHandler<GetPermissionCatalogQuery, Result<IReadOnlyList<PermissionResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPermissionCatalogQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<PermissionResponse>>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .OrderBy(p => p.Category).ThenBy(p => p.Label)
            .Select(p => new PermissionResponse(p.Id, p.Key, p.Category, p.Label))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PermissionResponse>>(permissions);
    }
}
