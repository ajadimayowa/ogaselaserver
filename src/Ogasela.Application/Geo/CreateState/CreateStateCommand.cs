using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.CreateState;

public sealed record CreateStateCommand(string Name, string Code) : IRequest<Result<StateResponse>>;
