namespace Ogasela.Domain.Moderation;

/// <summary>
/// A shared report record, generic across whatever gets reported (a listing, a user, a
/// message). Introduced in Phase 7 by ReportUserCommand (TargetType.User) so that Phase 11's
/// Moderation module can add reporting for listings/messages against the same table later,
/// without a data migration.
/// </summary>
public class Report
{
    private Report()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Null for a system-generated report (e.g. Phase 9's fraud-risk check on publish) - there's no human reporter to record.</summary>
    public Guid? ReporterId { get; private set; }

    public ReportTargetType TargetType { get; private set; }

    public Guid TargetId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public ReportStatus Status { get; private set; }

    /// <summary>0-1. Set only for a system-generated fraud-risk report (see Phase 9's ListingPublishService); null for a human-submitted report. Feeds Phase 11's moderation-queue priority score.</summary>
    public decimal? FraudScore { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Report Create(
        Guid? reporterId, ReportTargetType targetType, Guid targetId, string reason, DateTime now, decimal? fraudScore = null)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Status = ReportStatus.Open,
            FraudScore = fraudScore,
            CreatedAt = now
        };
    }

    public void Resolve()
    {
        Status = ReportStatus.Resolved;
    }

    public void Escalate()
    {
        Status = ReportStatus.Escalated;
    }
}
