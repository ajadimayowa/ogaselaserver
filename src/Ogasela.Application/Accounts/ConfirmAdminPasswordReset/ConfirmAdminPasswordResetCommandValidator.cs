using FluentValidation;

namespace Ogasela.Application.Accounts.ConfirmAdminPasswordReset;

public sealed class ConfirmAdminPasswordResetCommandValidator : AbstractValidator<ConfirmAdminPasswordResetCommand>
{
    public ConfirmAdminPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
