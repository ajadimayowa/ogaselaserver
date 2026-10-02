using FluentValidation;

namespace Ogasela.Application.Ai.GenerateDescription;

public sealed class GenerateDescriptionCommandValidator : AbstractValidator<GenerateDescriptionCommand>
{
    public GenerateDescriptionCommandValidator()
    {
        RuleFor(x => x.PromotionPlanId).NotEqual(Guid.Empty).When(x => x.PromotionPlanId is not null);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Keywords) || !string.IsNullOrWhiteSpace(x.ImageRef))
            .WithMessage("Provide either keywords or a reference image.");
    }
}
