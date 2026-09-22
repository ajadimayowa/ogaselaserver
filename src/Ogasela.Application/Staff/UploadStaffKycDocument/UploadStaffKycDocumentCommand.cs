using MediatR;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.UploadStaffKycDocument;

public sealed record UploadStaffKycDocumentCommand(
    Guid StaffProfileId,
    StaffKycDocumentType DocumentType,
    Stream Content,
    string FileName,
    string ContentType) : IRequest<Result<StaffKycDocumentResponse>>;
