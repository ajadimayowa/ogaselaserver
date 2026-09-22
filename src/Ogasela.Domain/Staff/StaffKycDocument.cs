namespace Ogasela.Domain.Staff;

/// <summary>One uploaded KYC file for a StaffProfile. A profile can have several (e.g. GovernmentId + ProofOfAddress).</summary>
public class StaffKycDocument
{
    private StaffKycDocument()
    {
    }

    public Guid Id { get; private set; }

    public Guid StaffProfileId { get; private set; }

    public StaffKycDocumentType DocumentType { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public DateTime UploadedAt { get; private set; }

    public static StaffKycDocument Create(
        Guid staffProfileId, StaffKycDocumentType documentType, string storageKey, string originalFileName, string contentType)
    {
        return new StaffKycDocument
        {
            Id = Guid.NewGuid(),
            StaffProfileId = staffProfileId,
            DocumentType = documentType,
            StorageKey = storageKey,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };
    }
}
