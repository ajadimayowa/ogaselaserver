using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetSuperAdmins;

public sealed class GetSuperAdminsQueryHandler : IRequestHandler<GetSuperAdminsQuery, Result<IReadOnlyList<SuperAdminResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSuperAdminsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<SuperAdminResponse>>> Handle(GetSuperAdminsQuery request, CancellationToken cancellationToken)
    {
        var response = await _dbContext.Users
            .Where(u => u.Role == UserRole.SuperAdmin)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new SuperAdminResponse(u.Id, u.Name, u.Email, u.Phone, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<SuperAdminResponse>>(response);
    }
}
