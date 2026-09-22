using FluentValidation;

namespace Ogasela.Application.AdIntegrations.PromoteListing;

public sealed class PromoteListingCommandValidator : AbstractValidator<PromoteListingCommand>
{
    public PromoteListingCommandValidator()
    {
        RuleFor(x => x.BudgetKobo).GreaterThan(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}
