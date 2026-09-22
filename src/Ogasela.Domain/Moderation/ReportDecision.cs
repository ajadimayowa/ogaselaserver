namespace Ogasela.Domain.Moderation;

/// <summary>A Moderator's resolution of an open Report.</summary>
public enum ReportDecision
{
    /// <summary>The report is upheld as valid; resolved with no further automated action on the target.</summary>
    Approve,

    /// <summary>The report is upheld and the reported content is taken down (a Listing is paused; see ResolveReportCommandHandler for what "remove" does per target type).</summary>
    Remove,

    /// <summary>Kicked up for higher-level attention rather than decided here.</summary>
    Escalate
}
