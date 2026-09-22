using FluentValidation;

namespace Ogasela.Application.Promotions.UpdatePromotionPlan;

public sealed class UpdatePromotionPlanCommandValidator : AbstractValidator<UpdatePromotionPlanCommand>
{
    public UpdatePromotionPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DurationDays).GreaterThan(0);
        RuleFor(x => x.PhotoLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BoostWeight).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BundledAdCreditKobo).GreaterThanOrEqualTo(0);
    }
}
