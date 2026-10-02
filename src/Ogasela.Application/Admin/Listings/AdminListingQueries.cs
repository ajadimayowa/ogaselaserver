using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.Listings;

/// <summary>
/// Every ad in any status for the Control Portal's All Ads page, newest first. Search matches the
/// ad title, the seller's business name or the seller's own name.
/// </summary>
public sealed record GetAdminListingsQuery(
    string? Search,
    ListingStatus? Status,
    Guid? CategoryId,
    string? Plan,
    bool ReportedOnly,
    int Page,
    int PageSize) : IRequest<Result<AdminListingPage>>;

public sealed record AdminListingListItem(
    Guid Id,
    string Title,
    decimal? Price,
    ListingStatus Status,
    string? ThumbnailUrl,
    string? CategoryName,
    string? PlanName,
    Guid SellerUserId,
    string SellerBusinessName,
    string? Location,
    int OpenReports,
    DateTime CreatedAt,
    DateTime? ExpiresAt);

public sealed record AdminListingPage(IReadOnlyList<AdminListingListItem> Items, int Page, int PageSize, int TotalCount);

public sealed record GetAdminListingDetailQuery(Guid ListingId) : IRequest<Result<AdminListingDetail>>;

public sealed record AdminListingDetail(
    Guid Id,
    string Title,
    string Description,
    decimal? Price,
    ListingCondition Condition,
    ListingStatus Status,
    IReadOnlyList<string> MediaUrls,
    string? Location,
    Guid CategoryId,
    string? CategoryName,
    string? ParentCategoryName,
    string? PlanName,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    AdminListingSeller Seller,
    AdminListingStats Stats,
    IReadOnlyList<AdminListingReport> Reports,
    IReadOnlyList<AdminListingCampaign> Campaigns,
    IReadOnlyList<AdminListingHistoryEntry> History);

public sealed record AdminListingSeller(
    Guid UserId, Guid SellerProfileId, string BusinessName, string? Name, string? Phone, string VerificationStatus, bool IsSuspended);

/// <summary>Lifetime on-platform engagement.</summary>
public sealed record AdminListingStats(long Impressions, long Views, long CallClicks, long MessageStarts, long Saves);

public sealed record AdminListingReport(Guid Id, string Reason, ReportStatus Status, decimal? FraudScore, bool SystemGenerated, DateTime CreatedAt);

public sealed record AdminListingCampaign(string Platform, string Status, long Impressions, long Clicks, decimal SpendKobo);

/// <summary>Admin actions taken on this ad, from the audit log, newest first.</summary>
public sealed record AdminListingHistoryEntry(string Action, DateTime At, string? Details);

public sealed class AdminListingQueryHandlers :
    IRequestHandler<GetAdminListingsQuery, Result<AdminListingPage>>,
    IRequestHandler<GetAdminListingDetailQuery, Result<AdminListingDetail>>
{
    private readonly IApplicationDbContext _dbContext;

    public AdminListingQueryHandlers(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminListingPage>> Handle(GetAdminListingsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query =
            from l in _dbContext.Listings
            join s in _dbContext.SellerProfiles on l.SellerId equals s.Id
            join u in _dbContext.Users on s.UserId equals u.Id
            select new { l, s, u };

        if (request.Status is { } status)
        {
            query = status == ListingStatus.Active
                // "Live" in the portal includes ads in their last few days.
                ? query.Where(x => x.l.Status == ListingStatus.Active || x.l.Status == ListingStatus.ExpiringSoon)
                : query.Where(x => x.l.Status == status);
        }

        if (request.CategoryId is { } categoryId)
        {
            var ids = _dbContext.Categories.Where(c => c.Id == categoryId || c.ParentCategoryId == categoryId).Select(c => c.Id);
            query = query.Where(x => ids.Contains(x.l.CategoryId));
        }

        if (request.Plan is { } plan)
        {
            var planIds = _dbContext.PromotionPlans.Where(p => p.Name == plan).Select(p => (Guid?)p.Id);
            query = query.Where(x => planIds.Contains(x.l.PromotionPlanId));
        }

        if (request.ReportedOnly)
        {
            var reported = _dbContext.Reports
                .Where(r => r.TargetType == ReportTargetType.Listing && r.Status == ReportStatus.Open)
                .Select(r => r.TargetId);
            query = query.Where(x => reported.Contains(x.l.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.l.Title.ToLower().Contains(term)
                || x.s.BusinessName.ToLower().Contains(term)
                || (x.u.Name != null && x.u.Name.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.l.CreatedAt)
            .ThenByDescending(x => x.l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var listingIds = rows.Select(r => r.l.Id).ToList();
        var categoryIds = rows.Select(r => r.l.CategoryId).Distinct().ToList();
        var categoryNames = await _dbContext.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var planNames = await _dbContext.PromotionPlans.ToDictionaryAsync(p => p.Id, p => p.Name.ToString(), cancellationToken);
        var reportCounts = await _dbContext.Reports
            .Where(r => r.TargetType == ReportTargetType.Listing && r.Status == ReportStatus.Open && listingIds.Contains(r.TargetId))
            .GroupBy(r => r.TargetId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var items = rows.Select(r => new AdminListingListItem(
                r.l.Id,
                r.l.Title,
                r.l.Price,
                r.l.Status,
                r.l.MediaUrls.FirstOrDefault(),
                categoryNames.GetValueOrDefault(r.l.CategoryId),
                r.l.PromotionPlanId is { } pid ? planNames.GetValueOrDefault(pid) : null,
                r.u.Id,
                r.s.BusinessName,
                r.l.Location,
                reportCounts.GetValueOrDefault(r.l.Id),
                r.l.CreatedAt,
                r.l.ExpiresAt))
            .ToList();

        return Result.Success(new AdminListingPage(items, page, pageSize, total));
    }

    public async Task<Result<AdminListingDetail>> Handle(GetAdminListingDetailQuery request, CancellationToken cancellationToken)
    {
        var row = await (
            from l in _dbContext.Listings
            join s in _dbContext.SellerProfiles on l.SellerId equals s.Id
            join u in _dbContext.Users on s.UserId equals u.Id
            where l.Id == request.ListingId
            select new { l, s, u })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result.Failure<AdminListingDetail>(AdminListingErrors.NotFound);
        }

        var listing = row.l;
        var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == listing.CategoryId, cancellationToken);
        var parentName = category?.ParentCategoryId is { } parentId
            ? await _dbContext.Categories.Where(c => c.Id == parentId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var planName = listing.PromotionPlanId is { } planId
            ? await _dbContext.PromotionPlans.Where(p => p.Id == planId).Select(p => p.Name.ToString()).FirstOrDefaultAsync(cancellationToken)
            : null;

        var stats = await _dbContext.ListingDailyStats
            .Where(s => s.ListingId == listing.Id)
            .GroupBy(s => s.ListingId)
            .Select(g => new AdminListingStats(
                g.Sum(s => s.Impressions), g.Sum(s => s.Views), g.Sum(s => s.CallClicks), g.Sum(s => s.MessageStarts), g.Sum(s => s.Saves)))
            .FirstOrDefaultAsync(cancellationToken) ?? new AdminListingStats(0, 0, 0, 0, 0);

        var reports = await _dbContext.Reports
            .Where(r => r.TargetType == ReportTargetType.Listing && r.TargetId == listing.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminListingReport(r.Id, r.Reason, r.Status, r.FraudScore, r.ReporterId == null, r.CreatedAt))
            .ToListAsync(cancellationToken);

        var campaigns = (await _dbContext.AdCampaigns
                .Where(c => c.ListingId == listing.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.Platform)
            .Select(g => g.First())
            .Select(c =>
            {
                var metrics = TryParse(c.MetricsSnapshotJson);
                return new AdminListingCampaign(c.Platform.ToString(), c.Status.ToString(), metrics.Impressions, metrics.Clicks, metrics.SpendKobo);
            })
            .ToList();

        var history = await _dbContext.AuditLogs
            .Where(a => a.TargetId == listing.Id && a.TargetType == nameof(Listing))
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new AdminListingHistoryEntry(a.Action, a.Timestamp, a.AfterJson))
            .ToListAsync(cancellationToken);

        return Result.Success(new AdminListingDetail(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.Condition,
            listing.Status,
            listing.MediaUrls,
            listing.Location,
            listing.CategoryId,
            category?.Name,
            parentName,
            planName,
            listing.ReviewNote,
            listing.CreatedAt,
            listing.UpdatedAt,
            listing.PublishedAt,
            listing.ExpiresAt,
            new AdminListingSeller(row.u.Id, row.s.Id, row.s.BusinessName, row.u.Name, row.u.Phone,
                row.s.VerificationStatus.ToString(), row.u.IsSuspended),
            stats,
            reports,
            campaigns,
            history));
    }

    private static AdMetricsSnapshot TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AdMetricsSnapshot>(json) ?? new AdMetricsSnapshot(0, 0, 0m);
        }
        catch (System.Text.Json.JsonException)
        {
            return new AdMetricsSnapshot(0, 0, 0m);
        }
    }
}

public static class AdminListingErrors
{
    public static readonly Error NotFound = new("AdminListings.NotFound", "This ad could not be found.");

    public static readonly Error NotLive = new("AdminListings.NotLive", "Only a live ad can be taken down.");

    public static readonly Error NotPaused = new("AdminListings.NotPaused", "Only a paused ad can be reinstated.");

    public static readonly Error CategoryNotFound = new("AdminListings.CategoryNotFound", "That category doesn't exist.");
}
