using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetUnits;

public sealed class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, Result<IReadOnlyList<OrgUnitResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetUnitsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<OrgUnitResponse>>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Units.AsQueryable();
        if (request.DepartmentId is { } departmentId)
        {
            query = query.Where(u => u.DepartmentId == departmentId);
        }

        var units = await query
            .OrderBy(u => u.Name)
            .Select(u => new OrgUnitResponse(u.Id, u.DepartmentId, u.Name, u.Description, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<OrgUnitResponse>>(units);
    }
}
