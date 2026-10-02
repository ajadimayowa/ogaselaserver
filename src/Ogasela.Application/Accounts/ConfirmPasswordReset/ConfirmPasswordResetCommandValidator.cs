using FluentValidation;

namespace Ogasela.Application.Accounts.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandValidator : AbstractValidator<ConfirmPasswordResetCommand>
{
    public ConfirmPasswordResetCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.Phone) != string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Provide either the phone number or the email the code was sent to.");
        When(x => !string.IsNullOrWhiteSpace(x.Email), () => RuleFor(x => x.Email).EmailAddress());
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
