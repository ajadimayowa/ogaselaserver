using FluentValidation;

namespace Ogasela.Application.Payments.FundWallet;

public sealed class FundWalletCommandValidator : AbstractValidator<FundWalletCommand>
{
    public FundWalletCommandValidator()
    {
        RuleFor(x => x.AmountKobo).GreaterThan(0);
    }
}
