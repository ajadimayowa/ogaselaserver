using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetStates;

public sealed class GetStatesQueryHandler : IRequestHandler<GetStatesQuery, Result<IReadOnlyList<StateResponse>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStatesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<StateResponse>>> Handle(GetStatesQuery request, CancellationToken cancellationToken)
    {
        var states = await _dbContext.NigeriaStates
            .OrderBy(s => s.Name)
            .Select(s => new StateResponse(s.Id, s.Name, s.Code, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<StateResponse>>(states);
    }
}
