using FluentValidation;

namespace Ogasela.Application.Accounts.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Must(NigerianPhoneNumber.IsValid)
            .WithMessage("Phone number must be a valid Nigerian mobile number.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("Verification code must be a 6-digit number.");
    }
}
