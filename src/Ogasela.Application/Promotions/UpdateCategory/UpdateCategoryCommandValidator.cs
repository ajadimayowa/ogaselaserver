using FluentValidation;

namespace Ogasela.Application.Promotions.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AttributeSchemaVersion).GreaterThan(0);

        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.ImageFileName).NotEmpty();
            RuleFor(x => x.ImageContentType).NotEmpty();
        });
    }
}
