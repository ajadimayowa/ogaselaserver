using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTime _dateTime;

    public ConfirmPasswordResetCommandHandler(
        IApplicationDbContext dbContext, IOtpService otpService, IPasswordHasher passwordHasher, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var byEmail = !string.IsNullOrWhiteSpace(request.Email);
        var otpKey = byEmail ? PasswordResetOtpKey.ForEmail(request.Email!) : PasswordResetOtpKey.ForPhone(request.Phone!);

        var otpResult = await _otpService.VerifyAsync(otpKey, request.Code, cancellationToken);
        var otpError = otpResult switch
        {
            OtpVerificationResult.Success => (Error?)null,
            OtpVerificationResult.InvalidCode => AccountErrors.OtpInvalid,
            OtpVerificationResult.Expired => AccountErrors.OtpExpired,
            OtpVerificationResult.RateLimited => AccountErrors.OtpRateLimited,
            _ => AccountErrors.OtpInvalid
        };

        if (otpError is not null)
        {
            return Result.Failure(otpError);
        }

        User? user;
        if (byEmail)
        {
            var email = request.Email!.Trim().ToLowerInvariant();
            user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email, cancellationToken);
        }
        else
        {
            user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Phone == otpKey, cancellationToken);
        }

        if (user is null)
        {
            return Result.Failure(AccountErrors.UserNotFound);
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword), mustChangePassword: false);

        // Someone resetting usually can't trust the old password any more - end every session it opened.
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
