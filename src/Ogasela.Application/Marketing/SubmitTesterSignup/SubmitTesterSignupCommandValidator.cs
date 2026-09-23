using FluentValidation;

namespace Ogasela.Application.Marketing.SubmitTesterSignup;

public sealed class SubmitTesterSignupCommandValidator : AbstractValidator<SubmitTesterSignupCommand>
{
    public SubmitTesterSignupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email address is not valid.");
        RuleFor(x => x.TurnstileToken).NotEmpty().WithMessage("Verification token is required.");
    }
}
