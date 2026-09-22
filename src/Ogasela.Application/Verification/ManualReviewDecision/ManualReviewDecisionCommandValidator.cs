using FluentValidation;
using Ogasela.Domain.Verification;

namespace Ogasela.Application.Verification.ManualReviewDecision;

public sealed class ManualReviewDecisionCommandValidator : AbstractValidator<ManualReviewDecisionCommand>
{
    public ManualReviewDecisionCommandValidator()
    {
        RuleFor(x => x.VerificationId).NotEmpty();

        RuleFor(x => x.Decision)
            .Must(d => d is VerificationDecision.Verified or VerificationDecision.Failed)
            .WithMessage("A manual review decision must be either Verified or Failed.");
    }
}
