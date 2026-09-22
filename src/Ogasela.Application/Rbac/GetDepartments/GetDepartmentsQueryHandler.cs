using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetDepartments;

public sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetDepartmentsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _dbContext.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.Description, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DepartmentResponse>>(departments);
    }
}
