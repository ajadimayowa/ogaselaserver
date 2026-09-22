using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.UpdateState;

public sealed class UpdateStateCommandHandler : IRequestHandler<UpdateStateCommand, Result<StateResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateStateCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StateResponse>> Handle(UpdateStateCommand request, CancellationToken cancellationToken)
    {
        var state = await _dbContext.NigeriaStates.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (state is null)
        {
            return Result.Failure<StateResponse>(GeoErrors.StateNotFound);
        }

        var nameOrCodeTaken = await _dbContext.NigeriaStates
            .AnyAsync(s => s.Id != request.Id && (s.Name == request.Name || s.Code == request.Code), cancellationToken);
        if (nameOrCodeTaken)
        {
            return Result.Failure<StateResponse>(GeoErrors.StateNameOrCodeTaken);
        }

        state.Update(request.Name, request.Code);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new StateResponse(state.Id, state.Name, state.Code, state.CreatedAt));
    }
}
