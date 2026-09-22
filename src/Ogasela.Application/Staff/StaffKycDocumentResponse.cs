namespace Ogasela.Application.Staff;

/// <summary>Never includes the raw StorageKey - that's an internal detail, not something the client should see or use directly.</summary>
public sealed record StaffKycDocumentResponse(Guid Id, string DocumentType, string OriginalFileName, DateTime UploadedAt);
