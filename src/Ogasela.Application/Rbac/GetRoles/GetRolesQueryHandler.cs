using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetRoles;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetRolesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var response = roles
            .Select(r => new RoleResponse(r.Id, r.Name, r.RoleType, r.PermissionKeys, r.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<RoleResponse>>(response);
    }
}
