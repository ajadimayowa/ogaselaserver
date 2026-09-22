using FluentValidation;

namespace Ogasela.Application.Accounts.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Phone) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Either a phone number or an email address is required.");

        RuleFor(x => x.Password).NotEmpty();
    }
}
