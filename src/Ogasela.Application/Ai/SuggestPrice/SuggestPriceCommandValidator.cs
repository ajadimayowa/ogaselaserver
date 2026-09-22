using FluentValidation;

namespace Ogasela.Application.Ai.SuggestPrice;

public sealed class SuggestPriceCommandValidator : AbstractValidator<SuggestPriceCommand>
{
    public SuggestPriceCommandValidator()
    {
        RuleFor(x => x.PromotionPlanId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
    }
}
