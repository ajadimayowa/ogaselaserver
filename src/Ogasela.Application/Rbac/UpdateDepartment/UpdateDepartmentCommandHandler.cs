using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateDepartmentCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (department is null)
        {
            return Result.Failure<DepartmentResponse>(RbacErrors.DepartmentNotFound);
        }

        var nameTaken = await _dbContext.Departments
            .AnyAsync(d => d.Id != request.Id && d.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<DepartmentResponse>(RbacErrors.DepartmentNameTaken);
        }

        department.Update(request.Name, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new DepartmentResponse(department.Id, department.Name, department.Description, department.CreatedAt));
    }
}
