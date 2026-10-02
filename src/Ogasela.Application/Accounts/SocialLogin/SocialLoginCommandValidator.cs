using FluentValidation;
using Ogasela.Domain.Accounts;

namespace Ogasela.Application.Accounts.SocialLogin;

public sealed class SocialLoginCommandValidator : AbstractValidator<SocialLoginCommand>
{
    public SocialLoginCommandValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.AccountType)
            .Must(role => role is UserRole.Buyer or UserRole.Seller)
            .WithMessage("AccountType must be Buyer or Seller.");
    }
}
