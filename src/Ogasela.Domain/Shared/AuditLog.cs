namespace Ogasela.Domain.Shared;

/// <summary>
/// A generic audit trail entry for administrative actions. Kept in the Shared namespace
/// because it's a cross-cutting record, not specific to any one module - Category/PromotionPlan
/// admin edits use it first, and Verification manual review and Moderation are expected to
/// reuse the same <c>IAuditLogger</c> service in later phases.
/// </summary>
public class AuditLog
{
    private AuditLog()
    {
    }

    public Guid Id { get; private set; }

    public Guid ActorId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string TargetType { get; private set; } = string.Empty;

    public Guid TargetId { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public DateTime Timestamp { get; private set; }

    public static AuditLog Create(
        Guid actorId, string action, string targetType, Guid targetId, string? beforeJson, string? afterJson, DateTime timestamp)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Timestamp = timestamp
        };
    }
}
