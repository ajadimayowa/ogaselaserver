using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateOrgUnit;

public sealed class UpdateOrgUnitCommandHandler : IRequestHandler<UpdateOrgUnitCommand, Result<OrgUnitResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateOrgUnitCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<OrgUnitResponse>> Handle(UpdateOrgUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<OrgUnitResponse>(RbacErrors.UnitNotFound);
        }

        var nameTaken = await _dbContext.Units
            .AnyAsync(u => u.Id != request.Id && u.DepartmentId == unit.DepartmentId && u.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<OrgUnitResponse>(RbacErrors.UnitNameTaken);
        }

        unit.Update(request.Name, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new OrgUnitResponse(unit.Id, unit.DepartmentId, unit.Name, unit.Description, unit.CreatedAt));
    }
}
