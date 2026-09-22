using FluentValidation;

namespace Ogasela.Application.Ai.EnhanceImage;

public sealed class EnhanceImageCommandValidator : AbstractValidator<EnhanceImageCommand>
{
    public EnhanceImageCommandValidator()
    {
        RuleFor(x => x.PromotionPlanId).NotEmpty();
        RuleFor(x => x.Image).NotNull();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
