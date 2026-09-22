using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.UpdateState;

public sealed record UpdateStateCommand(Guid Id, string Name, string Code) : IRequest<Result<StateResponse>>;
