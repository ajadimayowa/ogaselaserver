using FluentValidation;

namespace Ogasela.Application.Moderation.ReportListing;

public sealed class ReportListingCommandValidator : AbstractValidator<ReportListingCommand>
{
    public ReportListingCommandValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}
