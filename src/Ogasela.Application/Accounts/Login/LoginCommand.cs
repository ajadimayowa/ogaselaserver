using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Login;

public sealed record LoginCommand(string? Phone, string? Email, string Password) : IRequest<Result<AuthTokenResponse>>;
