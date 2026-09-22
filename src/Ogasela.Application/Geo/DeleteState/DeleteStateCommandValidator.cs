using FluentValidation;

namespace Ogasela.Application.Geo.DeleteState;

public sealed class DeleteStateCommandValidator : AbstractValidator<DeleteStateCommand>
{
    public DeleteStateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
