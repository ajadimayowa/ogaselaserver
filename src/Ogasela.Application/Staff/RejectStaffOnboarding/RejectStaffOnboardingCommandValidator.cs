using FluentValidation;

namespace Ogasela.Application.Staff.RejectStaffOnboarding;

public sealed class RejectStaffOnboardingCommandValidator : AbstractValidator<RejectStaffOnboardingCommand>
{
    public RejectStaffOnboardingCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
