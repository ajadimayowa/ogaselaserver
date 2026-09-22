using MediatR;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestOtp;

public sealed class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, Result>
{
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<RequestOtpCommandHandler> _logger;

    public RequestOtpCommandHandler(IOtpService otpService, ISmsSender smsSender, ILogger<RequestOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var code = await _otpService.GenerateAsync(request.Phone, cancellationToken);

        try
        {
            var message = $"Your Ogasela verification code is {code}. It expires in 5 minutes. Do not share this code with anyone.";
            await _smsSender.SendAsync(request.Phone, message, cancellationToken);
        }
        catch (Exception ex)
        {
            // The code is generated and stored regardless; a delivery hiccup shouldn't fail
            // the request; the client can call /otp/request again to retry delivery.
            _logger.LogError(ex, "Failed to send OTP SMS to {Phone}", request.Phone);
        }

        return Result.Success();
    }
}
