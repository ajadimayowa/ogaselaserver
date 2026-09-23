using FluentValidation;
using Ogasela.Domain.Listings;

namespace Ogasela.Application.Listings.CreateDraftListing;

public sealed class CreateDraftListingCommandValidator : AbstractValidator<CreateDraftListingCommand>
{
    public CreateDraftListingCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.CategoryId).NotEmpty();

        When(x => x.Price is not null, () =>
        {
            RuleFor(x => x.Price!.Value).GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x.MediaUrls).NotNull();
        RuleFor(x => x.MediaUrls).Must(m => m.Count <= Listing.MaxPhotoCount)
            .WithMessage($"A listing can have at most {Listing.MaxPhotoCount} photos.")
            .When(x => x.MediaUrls is not null);
        RuleForEach(x => x.MediaUrls).NotEmpty();
    }
}
