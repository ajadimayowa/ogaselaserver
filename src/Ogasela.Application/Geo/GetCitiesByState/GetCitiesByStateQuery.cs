using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetCitiesByState;

public sealed record GetCitiesByStateQuery(Guid StateId) : IRequest<Result<IReadOnlyList<CityResponse>>>;
