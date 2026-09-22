using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Rbac;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateDepartment;

public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateDepartmentCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DepartmentResponse>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var nameTaken = await _dbContext.Departments.AnyAsync(d => d.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            return Result.Failure<DepartmentResponse>(RbacErrors.DepartmentNameTaken);
        }

        var department = Department.Create(request.Name, request.Description);
        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new DepartmentResponse(department.Id, department.Name, department.Description, department.CreatedAt));
    }
}
