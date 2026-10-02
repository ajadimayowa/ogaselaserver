namespace Ogasela.Domain.Accounts;

public enum UserDocumentType
{
    IdCard,
    UtilityBill,

    /// <summary>Sellers: CAC certificate or other business registration document.</summary>
    BusinessRegistration,

    /// <summary>Sellers: proof of the business/office address, e.g. a utility bill.</summary>
    OfficeUtilityBill
}

/// <summary>The kinds of government ID a user can upload as their ID card.</summary>
public enum IdDocumentType
{
    NationalIdCard,
    NinSlip,
    VotersCard,
    DriversLicence,
    InternationalPassport
}

public enum UserDocumentStatus
{
    PendingReview,
    Approved,
    Rejected
}

/// <summary>
/// A document a user uploads about themselves (ID card, utility bill) or, for sellers, their
/// business (registration document, office utility bill) - stored privately, reviewed
/// by staff. Each upload is its own row; the newest of a type is the one that counts. IdType and
/// IdNumber are only set for an ID card.
/// </summary>
public class UserDocument
{
    private UserDocument()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public UserDocumentType Type { get; private set; }

    public IdDocumentType? IdType { get; private set; }

    public string? IdNumber { get; private set; }

    public string FileS3Key { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public UserDocumentStatus Status { get; private set; }

    /// <summary>Why staff rejected it (shown to the user); null otherwise.</summary>
    public string? ReviewNote { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static UserDocument Create(
        Guid userId, UserDocumentType type, IdDocumentType? idType, string? idNumber,
        string fileS3Key, string fileName, string contentType, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        IdType = idType,
        IdNumber = idNumber,
        FileS3Key = fileS3Key,
        FileName = fileName,
        ContentType = contentType,
        Status = UserDocumentStatus.PendingReview,
        CreatedAt = now
    };

    public void Approve(DateTime now)
    {
        Status = UserDocumentStatus.Approved;
        ReviewNote = null;
        ReviewedAt = now;
    }

    public void Reject(string reason, DateTime now)
    {
        Status = UserDocumentStatus.Rejected;
        ReviewNote = reason;
        ReviewedAt = now;
    }
}
