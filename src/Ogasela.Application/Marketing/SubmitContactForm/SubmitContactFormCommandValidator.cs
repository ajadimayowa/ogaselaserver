using FluentValidation;

namespace Ogasela.Application.Marketing.SubmitContactForm;

public sealed class SubmitContactFormCommandValidator : AbstractValidator<SubmitContactFormCommand>
{
    public SubmitContactFormCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email address is not valid.");
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.TurnstileToken).NotEmpty().WithMessage("Verification token is required.");
    }
}
