using FluentValidation;

namespace Ogasela.Application.Marketing.RequestAccountDeletion;

public sealed class RequestAccountDeletionCommandValidator : AbstractValidator<RequestAccountDeletionCommand>
{
    public RequestAccountDeletionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Either an email address or a phone number is required.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).EmailAddress().WithMessage("Email address is not valid.");
        });

        RuleFor(x => x.RequestType).IsInEnum();
        RuleFor(x => x.Details).MaximumLength(5000);
        RuleFor(x => x.TurnstileToken).NotEmpty().WithMessage("Verification token is required.");
    }
}
