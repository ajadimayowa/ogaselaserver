using FluentValidation;

namespace Ogasela.Application.Listings.UploadListingImage;

public sealed class UploadListingImageCommandValidator : AbstractValidator<UploadListingImageCommand>
{
    public UploadListingImageCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty().Must(c => c.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only image files are accepted.");
    }
}
