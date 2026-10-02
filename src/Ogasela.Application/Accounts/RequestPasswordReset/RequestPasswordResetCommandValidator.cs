using FluentValidation;

namespace Ogasela.Application.Accounts.RequestPasswordReset;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();

        When(x => x.Channel == PasswordResetChannel.Email, () =>
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Enter a valid email address.");
        });

        When(x => x.Channel == PasswordResetChannel.Phone, () =>
        {
            RuleFor(x => x.Phone)
                .NotEmpty()
                .Must(NigerianPhoneNumber.IsValid)
                .WithMessage("Phone number must be a valid Nigerian mobile number.");
        });
    }
}
