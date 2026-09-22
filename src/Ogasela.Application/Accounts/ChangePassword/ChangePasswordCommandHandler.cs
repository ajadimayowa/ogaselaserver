using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ChangePassword;

/// <summary>
/// Any authenticated user can change their own password, not just staff - so this lives under
/// Accounts rather than Staff. Also used to clear a staff account's forced-rotation flag
/// (User.MustChangePassword) that ApproveStaffOnboardingCommand sets when it generates and emails
/// a temporary password.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId!.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AccountErrors.UserNotFound);
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(AccountErrors.CurrentPasswordIncorrect);
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword), mustChangePassword: false);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
