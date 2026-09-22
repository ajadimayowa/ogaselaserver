using Microsoft.AspNetCore.Mvc;
using Ogasela.Domain.Staff;

namespace Ogasela.Api.Contracts.Staff;

public sealed record CreateStaffRequest(string Name, string? Phone, string? Email, Guid DepartmentId, Guid UnitId, Guid RoleId);

/// <summary>Bound from multipart/form-data - a plain settable-property class binds IFormFile reliably, unlike a record (see SubmitVerificationRequest).</summary>
public sealed class UploadStaffKycDocumentRequest
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; } = null!;

    [FromForm(Name = "documentType")]
    public StaffKycDocumentType DocumentType { get; set; }
}

public sealed record RejectStaffOnboardingRequest(string Reason);
