using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandHandler : IRequestHandler<ConfirmPasswordResetCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;

    public ConfirmPasswordResetCommandHandler(IApplicationDbContext dbContext, IOtpService otpService, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ConfirmPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var otpResult = await _otpService.VerifyAsync(request.Phone, request.Code, cancellationToken);
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

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AccountErrors.UserNotFound);
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword), mustChangePassword: false);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
