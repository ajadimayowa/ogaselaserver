using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Staff.Interfaces;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.UploadStaffKycDocument;

public sealed class UploadStaffKycDocumentCommandHandler : IRequestHandler<UploadStaffKycDocumentCommand, Result<StaffKycDocumentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IStaffKycDocumentStorage _documentStorage;

    public UploadStaffKycDocumentCommandHandler(IApplicationDbContext dbContext, IStaffKycDocumentStorage documentStorage)
    {
        _dbContext = dbContext;
        _documentStorage = documentStorage;
    }

    public async Task<Result<StaffKycDocumentResponse>> Handle(UploadStaffKycDocumentCommand request, CancellationToken cancellationToken)
    {
        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(s => s.Id == request.StaffProfileId, cancellationToken);

        if (staffProfile is null)
        {
            return Result.Failure<StaffKycDocumentResponse>(StaffErrors.StaffProfileNotFound);
        }

        if (staffProfile.Status != StaffOnboardingStatus.PendingApproval)
        {
            return Result.Failure<StaffKycDocumentResponse>(StaffErrors.AlreadyDecided);
        }

        var storageKey = await _documentStorage.UploadAsync(
            staffProfile.Id, request.FileName, request.ContentType, request.Content, cancellationToken);

        var document = StaffKycDocument.Create(staffProfile.Id, request.DocumentType, storageKey, request.FileName, request.ContentType);
        _dbContext.StaffKycDocuments.Add(document);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new StaffKycDocumentResponse(
            document.Id, document.DocumentType.ToString(), document.OriginalFileName, document.UploadedAt));
    }
}
