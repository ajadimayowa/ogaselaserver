using FluentValidation;

namespace Ogasela.Application.Verification.SubmitVerification;

public sealed class SubmitVerificationCommandValidator : AbstractValidator<SubmitVerificationCommand>
{
    public SubmitVerificationCommandValidator()
    {
        RuleFor(x => x.SelfieImage).NotNull();
        RuleFor(x => x.SelfieFileName).NotEmpty();
        RuleFor(x => x.SelfieContentType).NotEmpty();
        RuleFor(x => x.IdPhotoImage).NotNull();
        RuleFor(x => x.IdPhotoFileName).NotEmpty();
        RuleFor(x => x.IdPhotoContentType).NotEmpty();
        RuleFor(x => x.LivenessSessionRef).NotEmpty();
    }
}
