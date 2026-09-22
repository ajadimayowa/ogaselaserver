using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
