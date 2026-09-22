namespace Ogasela.Domain.Verification;

/// <summary>
/// An immutable audit record of a moderator's manual verification decision, kept independently
/// of <see cref="BiometricVerification"/> itself so the review trail survives even if the
/// verification record's own decision fields are later revised.
/// </summary>
public class VerificationAuditEntry
{
    private VerificationAuditEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid BiometricVerificationId { get; private set; }

    public Guid ReviewerId { get; private set; }

    public VerificationDecision Decision { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static VerificationAuditEntry Create(
        Guid biometricVerificationId, Guid reviewerId, VerificationDecision decision, string? notes, DateTime createdAt)
    {
        return new VerificationAuditEntry
        {
            Id = Guid.NewGuid(),
            BiometricVerificationId = biometricVerificationId,
            ReviewerId = reviewerId,
            Decision = decision,
            Notes = notes,
            CreatedAt = createdAt
        };
    }
}
