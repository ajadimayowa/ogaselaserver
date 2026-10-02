using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmAdminPasswordReset;

/// <summary>
/// Sets the new password and revokes every active refresh token for the account - a reset
/// usually means the old password may be known to someone else, so any session they opened with
/// it must end too. Also clears MustChangePassword: the admin has just chosen this password.
/// </summary>
public sealed class ConfirmAdminPasswordResetCommandHandler : IRequestHandler<ConfirmAdminPasswordResetCommand, Result>
{
    private static readonly HashSet<UserRole> AllowedRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin, UserRole.Staff];

    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTime _dateTime;

    public ConfirmAdminPasswordResetCommandHandler(
        IApplicationDbContext dbContext,
        IOtpService otpService,
        IPasswordHasher passwordHasher,
        IDateTime dateTime)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(ConfirmAdminPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Same error as a wrong code for an unknown/non-admin email, so this endpoint can't be
        // used to enumerate accounts either.
        if (user is null || !AllowedRoles.Contains(user.Role) || string.IsNullOrWhiteSpace(user.Phone))
        {
            return Result.Failure(AccountErrors.OtpInvalid);
        }

        var verifyResult = await _otpService.VerifyAsync(user.Phone, request.Code, cancellationToken);

        var errorResult = verifyResult switch
        {
            OtpVerificationResult.Success => (Error?)null,
            OtpVerificationResult.InvalidCode => AccountErrors.OtpInvalid,
            OtpVerificationResult.Expired => AccountErrors.OtpExpired,
            OtpVerificationResult.RateLimited => AccountErrors.OtpRateLimited,
            _ => AccountErrors.OtpInvalid
        };

        if (errorResult is not null)
        {
            return Result.Failure(errorResult);
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword), mustChangePassword: false);

        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(_dateTime.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
