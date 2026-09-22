using FluentValidation;
using Ogasela.Application.Accounts;

namespace Ogasela.Application.Staff.CreateStaff;

public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Phone) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Either a phone number or an email address is required.");

        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone!)
                .Must(NigerianPhoneNumber.IsValid)
                .WithMessage("Phone number must be a valid Nigerian mobile number.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).EmailAddress().WithMessage("Email address is not valid.");
        });

        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
