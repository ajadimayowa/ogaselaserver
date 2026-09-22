using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserResponse>>;
