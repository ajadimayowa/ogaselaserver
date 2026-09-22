using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokenResponse>>;
