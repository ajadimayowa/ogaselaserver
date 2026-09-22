using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.BlockUser;

public sealed record BlockUserCommand(Guid TargetUserId) : IRequest<Result>;
