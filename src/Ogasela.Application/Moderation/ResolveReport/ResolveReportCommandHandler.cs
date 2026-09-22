using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ResolveReport;

public sealed class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IAuditLogger _auditLogger;

    public ResolveReportCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _dbContext.Reports.FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(ModerationErrors.ReportNotFound);
        }

        if (report.Status != ReportStatus.Open)
        {
            return Result.Failure(ModerationErrors.ReportAlreadyDecided);
        }

        var beforeJson = JsonSerializer.Serialize(new { report.Status });

        switch (request.Decision)
        {
            case ReportDecision.Approve:
                report.Resolve();
                break;

            case ReportDecision.Remove:
                report.Resolve();
                await RemoveTargetAsync(report, cancellationToken);
                break;

            case ReportDecision.Escalate:
                report.Escalate();
                break;
        }

        var now = _dateTime.UtcNow;
        var afterJson = JsonSerializer.Serialize(new { report.Status, Decision = request.Decision.ToString(), request.Notes });

        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, $"Report.{request.Decision}", nameof(Report), report.Id, beforeJson, afterJson, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// "Remove" only has a concrete platform action defined for a Listing today (paused, taking
    /// it off public view) - there's no user-suspension/ban capability anywhere yet in the
    /// domain model, so a User-target removal is resolved and audited like any other decision
    /// but doesn't (yet) change anything on the User record itself. Adding real account
    /// suspension is future scope, not invented here.
    /// </summary>
    private async Task RemoveTargetAsync(Report report, CancellationToken cancellationToken)
    {
        if (report.TargetType != ReportTargetType.Listing)
        {
            return;
        }

        var listing = await _dbContext.Listings.FirstOrDefaultAsync(l => l.Id == report.TargetId, cancellationToken);
        listing?.Pause(_dateTime.UtcNow);
    }
}
