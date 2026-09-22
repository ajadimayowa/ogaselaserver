using MediatR;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.VerifyOtp;

public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
{
    private readonly IOtpService _otpService;

    public VerifyOtpCommandHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await _otpService.VerifyAsync(request.Phone, request.Code, cancellationToken);

        return result switch
        {
            OtpVerificationResult.Success => Result.Success(),
            OtpVerificationResult.InvalidCode => Result.Failure(AccountErrors.OtpInvalid),
            OtpVerificationResult.Expired => Result.Failure(AccountErrors.OtpExpired),
            OtpVerificationResult.RateLimited => Result.Failure(AccountErrors.OtpRateLimited),
            _ => Result.Failure(AccountErrors.OtpInvalid)
        };
    }
}
