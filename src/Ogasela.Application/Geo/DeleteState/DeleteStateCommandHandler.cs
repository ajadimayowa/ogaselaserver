using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.DeleteState;

public sealed class DeleteStateCommandHandler : IRequestHandler<DeleteStateCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteStateCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteStateCommand request, CancellationToken cancellationToken)
    {
        var state = await _dbContext.NigeriaStates.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (state is null)
        {
            return Result.Failure(GeoErrors.StateNotFound);
        }

        var cities = await _dbContext.NigeriaCities
            .Where(c => c.StateId == request.Id)
            .ToListAsync(cancellationToken);
        _dbContext.NigeriaCities.RemoveRange(cities);

        _dbContext.NigeriaStates.Remove(state);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
