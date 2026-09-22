using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Geo;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.CreateState;

public sealed class CreateStateCommandHandler : IRequestHandler<CreateStateCommand, Result<StateResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateStateCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StateResponse>> Handle(CreateStateCommand request, CancellationToken cancellationToken)
    {
        var nameOrCodeTaken = await _dbContext.NigeriaStates
            .AnyAsync(s => s.Name == request.Name || s.Code == request.Code, cancellationToken);
        if (nameOrCodeTaken)
        {
            return Result.Failure<StateResponse>(GeoErrors.StateNameOrCodeTaken);
        }

        var state = NigeriaState.Create(request.Name, request.Code);
        _dbContext.NigeriaStates.Add(state);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new StateResponse(state.Id, state.Name, state.Code, state.CreatedAt));
    }
}
