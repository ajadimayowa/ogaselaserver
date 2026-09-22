using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.ReportUser;

public sealed record ReportUserCommand(Guid TargetUserId, string Reason) : IRequest<Result<Guid>>;
