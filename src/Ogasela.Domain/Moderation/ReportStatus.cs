namespace Ogasela.Domain.Moderation;

public enum ReportStatus
{
    Open,
    Resolved,
    Dismissed,

    /// <summary>Reviewed by a Moderator and kicked up for higher-level attention (Phase 11's ResolveReportCommand) - still counts as decided, so it drops out of the open moderation queue.</summary>
    Escalated
}
