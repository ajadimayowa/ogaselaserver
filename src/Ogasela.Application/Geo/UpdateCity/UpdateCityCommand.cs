using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.UpdateCity;

public sealed record UpdateCityCommand(Guid Id, string Name) : IRequest<Result<CityResponse>>;
