using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.VerifyAdminLoginOtp;

public sealed class VerifyAdminLoginOtpCommandHandler : IRequestHandler<VerifyAdminLoginOtpCommand, Result<AuthTokenResponse>>
{
    private static readonly HashSet<UserRole> AllowedRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin];

    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly AuthTokenIssuer _tokenIssuer;

    public VerifyAdminLoginOtpCommandHandler(IApplicationDbContext dbContext, IOtpService otpService, AuthTokenIssuer tokenIssuer)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<AuthTokenResponse>> Handle(VerifyAdminLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !AllowedRoles.Contains(user.Role) || string.IsNullOrWhiteSpace(user.Phone))
        {
            return Result.Failure<AuthTokenResponse>(AccountErrors.InvalidCredentials);
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
            return Result.Failure<AuthTokenResponse>(errorResult);
        }

        var response = await _tokenIssuer.IssueAsync(user, tokenToRotate: null, cancellationToken);
        return Result.Success(response);
    }
}
