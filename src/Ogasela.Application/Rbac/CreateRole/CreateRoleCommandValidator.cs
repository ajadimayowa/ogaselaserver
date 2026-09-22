using FluentValidation;

namespace Ogasela.Application.Rbac.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RoleType).IsInEnum();
        RuleForEach(x => x.PermissionKeys).NotEmpty();
    }
}
