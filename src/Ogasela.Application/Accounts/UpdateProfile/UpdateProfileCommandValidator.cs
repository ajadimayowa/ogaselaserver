using FluentValidation;

namespace Ogasela.Application.Accounts.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter your name.").MaximumLength(200);
    }
}
