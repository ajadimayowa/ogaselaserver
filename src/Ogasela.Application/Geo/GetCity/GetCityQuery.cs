using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetCity;

public sealed record GetCityQuery(Guid Id) : IRequest<Result<CityResponse>>;
