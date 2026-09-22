using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.GetState;

public sealed record GetStateQuery(Guid Id) : IRequest<Result<StateResponse>>;
