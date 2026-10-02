using FluentValidation;

namespace Ogasela.Application.Moderation.ListingReview;

public sealed class RejectListingCommandValidator : AbstractValidator<RejectListingCommand>
{
    public RejectListingCommandValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Tell the seller why the ad was rejected.")
            .MaximumLength(1000);
    }
}
