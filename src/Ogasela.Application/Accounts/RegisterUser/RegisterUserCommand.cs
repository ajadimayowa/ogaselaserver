using MediatR;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RegisterUser;

public sealed record RegisterUserCommand(
    string? Phone,
    string? Email,
    string Password,
    UserRole AccountType,
    string? BusinessName,
    string? RcNumber,
    string? Nin) : IRequest<Result<RegisterUserResponse>>;
