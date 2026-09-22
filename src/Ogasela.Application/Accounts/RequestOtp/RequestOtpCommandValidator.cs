using FluentValidation;

namespace Ogasela.Application.Accounts.RequestOtp;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(NigerianPhoneNumber.IsValid)
            .WithMessage("Phone number must be a valid Nigerian mobile number.");
    }
}
