using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetStates;

public sealed record GetStatesQuery : IRequest<Result<IReadOnlyList<StateResponse>>>;
