using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetStaffDetail;

public sealed class GetStaffDetailQueryHandler : IRequestHandler<GetStaffDetailQuery, Result<StaffDetailResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStaffDetailQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StaffDetailResponse>> Handle(GetStaffDetailQuery request, CancellationToken cancellationToken)
    {
        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(s => s.Id == request.StaffProfileId, cancellationToken);

        if (staffProfile is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.StaffProfileNotFound);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == staffProfile.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.UserNotFound);
        }

        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == staffProfile.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.DepartmentNotFound);
        }

        var unit = await _dbContext.Units.FirstOrDefaultAsync(u => u.Id == staffProfile.UnitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.UnitNotFound);
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == staffProfile.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<StaffDetailResponse>(StaffErrors.RoleNotFound);
        }

        var documents = await _dbContext.StaffKycDocuments
            .Where(d => d.StaffProfileId == staffProfile.Id)
            .OrderBy(d => d.UploadedAt)
            .Select(d => new StaffKycDocumentResponse(d.Id, d.DocumentType.ToString(), d.OriginalFileName, d.UploadedAt))
            .ToListAsync(cancellationToken);

        var response = new StaffDetailResponse(
            staffProfile.Id,
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            department.Name,
            unit.Name,
            role.Name,
            staffProfile.Status,
            staffProfile.CreatedAt,
            staffProfile.RejectionReason,
            staffProfile.DecidedAt,
            documents);

        return Result.Success(response);
    }
}
