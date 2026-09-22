using FluentValidation;

namespace Ogasela.Application.Rbac.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleForEach(x => x.PermissionKeys).NotEmpty();
    }
}
