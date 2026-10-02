namespace Ogasela.Domain.Moderation;

public enum DisputeReason
{
    ItemNotAsDescribed,
    NotDelivered,
    PaidNoResponse,
    FakeOrCounterfeit,
    BuyerDidNotPay,
    Other
}

public enum DisputeStatus
{
    /// <summary>Raised; no staff member has picked it up yet.</summary>
    Open,

    /// <summary>A staff member is working on it.</summary>
    InReview,

    Resolved
}

public enum DisputeOutcome
{
    InFavourOfRaiser,
    InFavourOfRespondent,
    MutualAgreement,
    Dismissed
}

/// <summary>
/// A buyer-seller disagreement about a deal on one listing, raised by either side against the
/// other and worked by staff. Evidence files are private-storage keys. Reference is the short code
/// both users and staff quote (e.g. DSP-7K3Q9M).
/// </summary>
public class Dispute
{
    private Dispute()
    {
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid ListingId { get; private set; }

    public Guid? ConversationId { get; private set; }

    public Guid RaisedByUserId { get; private set; }

    public Guid AgainstUserId { get; private set; }

    public DisputeReason Reason { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public List<string> EvidenceKeys { get; private set; } = new();

    public DisputeStatus Status { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public DisputeOutcome? Outcome { get; private set; }

    public string? ResolutionNote { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public static Dispute Raise(
        string reference, Guid listingId, Guid? conversationId, Guid raisedByUserId, Guid againstUserId,
        DisputeReason reason, string description, IEnumerable<string> evidenceKeys, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Reference = reference,
        ListingId = listingId,
        ConversationId = conversationId,
        RaisedByUserId = raisedByUserId,
        AgainstUserId = againstUserId,
        Reason = reason,
        Description = description,
        EvidenceKeys = evidenceKeys.ToList(),
        Status = DisputeStatus.Open,
        CreatedAt = now,
        UpdatedAt = now
    };

    public bool IsParty(Guid userId) => userId == RaisedByUserId || userId == AgainstUserId;

    public void AssignTo(Guid staffUserId, DateTime now)
    {
        AssignedToUserId = staffUserId;
        if (Status == DisputeStatus.Open)
        {
            Status = DisputeStatus.InReview;
        }

        UpdatedAt = now;
    }

    public void Touch(DateTime now) => UpdatedAt = now;

    public void Resolve(DisputeOutcome outcome, string note, Guid staffUserId, DateTime now)
    {
        Status = DisputeStatus.Resolved;
        Outcome = outcome;
        ResolutionNote = note;
        AssignedToUserId ??= staffUserId;
        ResolvedAt = now;
        UpdatedAt = now;
    }
}

public enum DisputeAuthorRole
{
    Raiser,
    Respondent,
    Staff
}

/// <summary>One entry in a dispute's thread - from either party or from staff. All three can see every entry.</summary>
public class DisputeMessage
{
    private DisputeMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid DisputeId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public DisputeAuthorRole AuthorRole { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>Optional private-storage key of an attached image or PDF.</summary>
    public string? AttachmentKey { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static DisputeMessage Create(
        Guid disputeId, Guid authorUserId, DisputeAuthorRole role, string body, string? attachmentKey, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        DisputeId = disputeId,
        AuthorUserId = authorUserId,
        AuthorRole = role,
        Body = body,
        AttachmentKey = attachmentKey,
        CreatedAt = now
    };
}
