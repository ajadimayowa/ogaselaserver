using FluentValidation;

namespace Ogasela.Application.Promotions.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AttributeSchemaVersion).GreaterThan(0);

        // A top-level category is always created together with its subcategories (sellers usually
        // post under one, though an ad may also sit directly in the category); a subcategory itself
        // can't have any - the tree is two levels.
        When(x => x.ParentCategoryId is null, () =>
        {
            RuleFor(x => x.SubcategoryNames)
                .NotEmpty().WithMessage("Add at least one subcategory.")
                .Must(names => names.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count)
                .WithMessage("Subcategory names must be unique.");
            RuleForEach(x => x.SubcategoryNames).NotEmpty().MaximumLength(100);
        });

        When(x => x.ParentCategoryId is not null, () =>
        {
            RuleFor(x => x.SubcategoryNames).Empty().WithMessage("A subcategory can't have subcategories of its own.");
        });

        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.ImageFileName).NotEmpty();
            RuleFor(x => x.ImageContentType).NotEmpty();
        });
    }
}
