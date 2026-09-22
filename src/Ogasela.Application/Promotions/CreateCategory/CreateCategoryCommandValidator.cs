using FluentValidation;

namespace Ogasela.Application.Promotions.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AttributeSchemaVersion).GreaterThan(0);

        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.ImageFileName).NotEmpty();
            RuleFor(x => x.ImageContentType).NotEmpty();
        });
    }
}
