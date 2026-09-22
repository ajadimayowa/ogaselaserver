using Ogasela.Domain.Accounts;

namespace Ogasela.Application.Accounts.RegisterUser;

public sealed record RegisterUserResponse(Guid UserId, UserRole Role);
