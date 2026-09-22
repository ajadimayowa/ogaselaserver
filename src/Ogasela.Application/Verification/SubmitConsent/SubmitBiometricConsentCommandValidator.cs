using FluentValidation;

namespace Ogasela.Application.Verification.SubmitConsent;

public sealed class SubmitBiometricConsentCommandValidator : AbstractValidator<SubmitBiometricConsentCommand>
{
    public SubmitBiometricConsentCommandValidator()
    {
        RuleFor(x => x.IpAddress).NotEmpty();
    }
}
