using FluentValidation;

namespace Ogasela.Application.Accounts.RequestAdminPasswordReset;

public sealed class RequestAdminPasswordResetCommandValidator : AbstractValidator<RequestAdminPasswordResetCommand>
{
    public RequestAdminPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
