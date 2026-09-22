using FluentValidation;

namespace Ogasela.Application.Geo.DeleteCity;

public sealed class DeleteCityCommandValidator : AbstractValidator<DeleteCityCommand>
{
    public DeleteCityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
