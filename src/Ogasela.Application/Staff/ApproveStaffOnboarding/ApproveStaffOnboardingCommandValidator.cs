using FluentValidation;

namespace Ogasela.Application.Staff.ApproveStaffOnboarding;

public sealed class ApproveStaffOnboardingCommandValidator : AbstractValidator<ApproveStaffOnboardingCommand>
{
    public ApproveStaffOnboardingCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
    }
}
