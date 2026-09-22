using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    /// <summary>Internal roles must go through AdminLoginCommand's email+password -> OTP flow instead - never issued a token straight off a password check, however this endpoint is called.</summary>
    private static readonly HashSet<UserRole> MfaRequiredRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin];

    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthTokenIssuer _tokenIssuer;

    public LoginCommandHandler(IApplicationDbContext dbContext, IPasswordHasher passwordHasher, AuthTokenIssuer tokenIssuer)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<AuthTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            u => (request.Phone != null && u.Phone == request.Phone) ||
                 (request.Email != null && u.Email == request.Email),
            cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.InvalidCredentials);
        }

        if (MfaRequiredRoles.Contains(user.Role))
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.MfaRequired);
        }

        var response = await _tokenIssuer.IssueAsync(user, tokenToRotate: null, cancellationToken);
        return Result.Success(response);
    }
}
