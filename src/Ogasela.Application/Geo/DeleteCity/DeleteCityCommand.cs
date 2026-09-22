using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.DeleteCity;

public sealed record DeleteCityCommand(Guid Id) : IRequest<Result>;
