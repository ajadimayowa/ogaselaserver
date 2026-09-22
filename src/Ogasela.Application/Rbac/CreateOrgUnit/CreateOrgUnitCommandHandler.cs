using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Rbac;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateOrgUnit;

public sealed class CreateOrgUnitCommandHandler : IRequestHandler<CreateOrgUnitCommand, Result<OrgUnitResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateOrgUnitCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrgUnitResponse>> Handle(CreateOrgUnitCommand request, CancellationToken cancellationToken)
    {
        var departmentExists = await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            return Result.Failure<OrgUnitResponse>(RbacErrors.DepartmentNotFound);
        }

        var nameTaken = await _dbContext.Units
            .AnyAsync(u => u.DepartmentId == request.DepartmentId && u.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<OrgUnitResponse>(RbacErrors.UnitNameTaken);
        }

        var unit = OrgUnit.Create(request.DepartmentId, request.Name, request.Description);
        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrgUnitResponse(unit.Id, unit.DepartmentId, unit.Name, unit.Description, unit.CreatedAt));
    }
}
