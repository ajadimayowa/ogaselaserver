using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetStaffList;

public sealed class GetStaffListQueryHandler : IRequestHandler<GetStaffListQuery, Result<IReadOnlyList<StaffListItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStaffListQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<StaffListItemResponse>>> Handle(GetStaffListQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.StaffProfiles.AsQueryable();

        if (request.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        var response = await (
            from staffProfile in query
            join user in _dbContext.Users on staffProfile.UserId equals user.Id
            join department in _dbContext.Departments on staffProfile.DepartmentId equals department.Id
            join unit in _dbContext.Units on staffProfile.UnitId equals unit.Id
            join role in _dbContext.Roles on staffProfile.RoleId equals role.Id
            orderby staffProfile.CreatedAt descending
            select new StaffListItemResponse(
                staffProfile.Id,
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                department.Name,
                unit.Name,
                role.Name,
                staffProfile.Status,
                staffProfile.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StaffListItemResponse>>(response);
    }
}
