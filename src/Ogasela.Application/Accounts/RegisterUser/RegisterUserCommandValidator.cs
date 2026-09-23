using FluentValidation;
using Ogasela.Domain.Accounts;

namespace Ogasela.Application.Accounts.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
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

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.AccountType)
            .Must(role => role is UserRole.Buyer or UserRole.Seller)
            .WithMessage("Account type must be Buyer or Seller.");

        When(x => x.AccountType == UserRole.Seller, () =>
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required when registering as a seller.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name!).MaximumLength(200);
        });
    }
}
