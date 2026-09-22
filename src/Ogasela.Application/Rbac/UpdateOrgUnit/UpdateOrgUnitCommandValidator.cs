using FluentValidation;

namespace Ogasela.Application.Rbac.UpdateOrgUnit;

public sealed class UpdateOrgUnitCommandValidator : AbstractValidator<UpdateOrgUnitCommand>
{
    public UpdateOrgUnitCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
