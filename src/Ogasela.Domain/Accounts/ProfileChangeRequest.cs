namespace Ogasela.Domain.Accounts;

public enum ProfileChangeType
{
    Phone,
    Email,
    BusinessInfo
}

public enum ProfileChangeStatus
{
    PendingReview,
    Approved,
    Rejected,
    Cancelled
}

/// <summary>
/// A change to a user's phone, email or (sellers) business name/store address that only takes
/// effect once staff approve it. NewValue holds the new phone or email (already verified with a
/// code sent to it); BusinessName/StoreAddress hold the proposed business details. A user has at
/// most one pending request per type - submitting another cancels the earlier one.
/// </summary>
public class ProfileChangeRequest
{
    private ProfileChangeRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public ProfileChangeType Type { get; private set; }

    public string? NewValue { get; private set; }

    public string? BusinessName { get; private set; }

    /// <summary>An empty string means "clear the store address".</summary>
    public string? StoreAddress { get; private set; }

    public ProfileChangeStatus Status { get; private set; }

    /// <summary>Why staff rejected it (shown to the user); null otherwise.</summary>
    public string? ReviewNote { get; private set; }

    public Guid? ReviewedById { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static ProfileChangeRequest ForContact(Guid userId, ProfileChangeType type, string newValue, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        NewValue = newValue,
        Status = ProfileChangeStatus.PendingReview,
        CreatedAt = now
    };

    public static ProfileChangeRequest ForBusinessInfo(Guid userId, string businessName, string storeAddress, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = ProfileChangeType.BusinessInfo,
        BusinessName = businessName,
        StoreAddress = storeAddress,
        Status = ProfileChangeStatus.PendingReview,
        CreatedAt = now
    };

    public void Approve(Guid reviewerId, DateTime now)
    {
        Status = ProfileChangeStatus.Approved;
        ReviewNote = null;
        ReviewedById = reviewerId;
        ReviewedAt = now;
    }

    public void Reject(Guid reviewerId, string reason, DateTime now)
    {
        Status = ProfileChangeStatus.Rejected;
        ReviewNote = reason;
        ReviewedById = reviewerId;
        ReviewedAt = now;
    }

    public void Cancel() => Status = ProfileChangeStatus.Cancelled;
}
