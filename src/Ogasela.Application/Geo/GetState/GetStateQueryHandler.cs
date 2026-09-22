using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetState;

public sealed class GetStateQueryHandler : IRequestHandler<GetStateQuery, Result<StateResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetStateQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StateResponse>> Handle(GetStateQuery request, CancellationToken cancellationToken)
    {
        var state = await _dbContext.NigeriaStates.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (state is null)
        {
            return Result.Failure<StateResponse>(GeoErrors.StateNotFound);
        }

        return Result.Success(new StateResponse(state.Id, state.Name, state.Code, state.CreatedAt));
    }
}
