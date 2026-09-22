using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Geo.DeleteState;

public sealed record DeleteStateCommand(Guid Id) : IRequest<Result>;
