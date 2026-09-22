using FluentValidation;

namespace Ogasela.Application.Staff.UploadStaffKycDocument;

public sealed class UploadStaffKycDocumentCommandValidator : AbstractValidator<UploadStaffKycDocumentCommand>
{
    public UploadStaffKycDocumentCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
