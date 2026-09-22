using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.DeleteCity;

public sealed class DeleteCityCommandHandler : IRequestHandler<DeleteCityCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteCityCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
        var city = await _dbContext.NigeriaCities.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (city is null)
        {
            return Result.Failure(GeoErrors.CityNotFound);
        }

        _dbContext.NigeriaCities.Remove(city);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
