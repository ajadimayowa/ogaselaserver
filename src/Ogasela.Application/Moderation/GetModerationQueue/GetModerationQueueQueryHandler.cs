using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Verification;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.GetModerationQueue;

/// <summary>
/// Merges Phase 7/9's open Report rows (including system-generated fraud-risk reports) and
/// Phase 2's pending biometric manual-review rows into one list, ranked by a simple, tunable
/// priority score:
///
///   Report:        ReportCountWeight * (open reports sharing this target) + FraudScoreWeight * (FraudScore ?? 0) + AgeWeight * ageInHours
///   Manual review: ManualReviewBaseScore + AgeWeight * ageInHours
///
/// Rationale for the weights below: multiple independent reports against the same target is the
/// strongest signal a human is actually looking at real abuse, so it's weighted highest. A fraud
/// score is a strong automated signal but less trusted than corroborated human reports, so it's
/// weighted a little lower (and is 0-1, capping its contribution at FraudScoreWeight). Identity
/// verification gets a moderate flat baseline so it competes fairly against low-signal reports
/// rather than being starved by them. Age contributes least on its own, but ensures nothing sits
/// in the queue forever - tune these constants freely as real triage patterns emerge.
/// </summary>
public sealed class GetModerationQueueQueryHandler
    : IRequestHandler<GetModerationQueueQuery, Result<IReadOnlyList<ModerationQueueItemResponse>>>
{
    private const decimal ReportCountWeight = 10m;
    private const decimal FraudScoreWeight = 20m;
    private const decimal ManualReviewBaseScore = 15m;
    private const decimal AgeWeightPerHour = 0.1m;

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public GetModerationQueueQueryHandler(IApplicationDbContext dbContext, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public async Task<Result<IReadOnlyList<ModerationQueueItemResponse>>> Handle(
        GetModerationQueueQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var openReports = await _dbContext.Reports
            .Where(r => r.Status == ReportStatus.Open)
            .ToListAsync(cancellationToken);

        var reportCountsByTarget = openReports
            .GroupBy(r => (r.TargetType, r.TargetId))
            .ToDictionary(g => g.Key, g => g.Count());

        var pendingReviews = await _dbContext.BiometricVerifications
            .Where(v => v.Decision == VerificationDecision.ManualReview)
            .ToListAsync(cancellationToken);

        var items = new List<ModerationQueueItemResponse>(openReports.Count + pendingReviews.Count);

        foreach (var report in openReports)
        {
            var ageInHours = (decimal)(now - report.CreatedAt).TotalHours;
            var reportCount = reportCountsByTarget[(report.TargetType, report.TargetId)];
            var score = (ReportCountWeight * reportCount) + (FraudScoreWeight * (report.FraudScore ?? 0m)) + (AgeWeightPerHour * ageInHours);

            items.Add(new ModerationQueueItemResponse(
                report.Id, ModerationQueueItemType.Report, report.TargetId, report.Reason, score, report.CreatedAt));
        }

        foreach (var verification in pendingReviews)
        {
            var ageInHours = (decimal)(now - verification.CreatedAt).TotalHours;
            var score = ManualReviewBaseScore + (AgeWeightPerHour * ageInHours);

            items.Add(new ModerationQueueItemResponse(
                verification.Id, ModerationQueueItemType.BiometricManualReview, verification.SellerId,
                "Biometric verification pending manual review", score, verification.CreatedAt));
        }

        return Result.Success<IReadOnlyList<ModerationQueueItemResponse>>(
            items.OrderByDescending(i => i.PriorityScore).ToList());
    }
}
