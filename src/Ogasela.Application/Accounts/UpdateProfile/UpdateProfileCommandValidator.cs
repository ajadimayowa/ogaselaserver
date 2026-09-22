using FluentValidation;

namespace Ogasela.Application.Accounts.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Phone) || !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.BusinessName))
            .WithMessage("At least one field must be provided to update.");

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

        When(x => !string.IsNullOrWhiteSpace(x.BusinessName), () =>
        {
            RuleFor(x => x.BusinessName!).MaximumLength(200);
        });
    }
}
