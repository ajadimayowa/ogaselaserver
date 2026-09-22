using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Payments;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.GetPlatformDashboard;

/// <summary>
/// Every count here is a best-effort proxy built from data the app already tracks, not a
/// dedicated analytics/event pipeline - see the DAU/MAU comment below for the least obvious one.
/// </summary>
public sealed class GetPlatformDashboardQueryHandler : IRequestHandler<GetPlatformDashboardQuery, Result<PlatformDashboardResponse>>
{
    /// <summary>
    /// Must mirror AuthTokenIssuer.RefreshTokenLifetime. There's no dedicated "last active"
    /// timestamp anywhere in the schema, but every refresh token is minted with this fixed
    /// lifetime - and, because the client silently rotates it on every access-token refresh
    /// (roughly every AccessTokenExpiryMinutes of continued use), a user's most recent
    /// RefreshToken.ExpiresAt reliably reconstructs their most recent activity as
    /// (ExpiresAt - RefreshTokenLifetime). "Active in the last N" is therefore just
    /// "has any RefreshToken whose ExpiresAt hasn't yet fallen more than N below its full lifetime".
    /// </summary>
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public GetPlatformDashboardQueryHandler(IApplicationDbContext dbContext, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public async Task<Result<PlatformDashboardResponse>> Handle(GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var activeUsersLast24h = await CountDistinctActiveUsersAsync(now, TimeSpan.FromHours(24), cancellationToken);
        var activeUsersLast30d = await CountDistinctActiveUsersAsync(now, TimeSpan.FromDays(30), cancellationToken);

        var listingsCreatedLast24h = await _dbContext.Listings.CountAsync(l => l.CreatedAt >= now.AddHours(-24), cancellationToken);
        var listingsCreatedLast30d = await _dbContext.Listings.CountAsync(l => l.CreatedAt >= now.AddDays(-30), cancellationToken);

        var revenueByPlan = await _dbContext.Transactions
            .Where(t => t.Type == TransactionType.PlanPurchase && t.Status == TransactionStatus.Success && t.RelatedListingId != null)
            .Join(_dbContext.Listings, t => t.RelatedListingId!.Value, l => l.Id, (t, l) => new { t.AmountKobo, l.PromotionPlanId })
            .Where(x => x.PromotionPlanId != null)
            .Join(_dbContext.PromotionPlans, x => x.PromotionPlanId!.Value, p => p.Id, (x, p) => new { x.AmountKobo, p.Name })
            .GroupBy(x => x.Name)
            .Select(g => new PlanRevenue(g.Key, g.Sum(x => x.AmountKobo), g.Count()))
            .ToListAsync(cancellationToken);

        var verificationFunnel = await BuildVerificationFunnelAsync(cancellationToken);
        var adBoostAdoptionRate = await ComputeAdBoostAdoptionRateAsync(cancellationToken);

        return Result.Success(new PlatformDashboardResponse(
            activeUsersLast24h, activeUsersLast30d, listingsCreatedLast24h, listingsCreatedLast30d,
            revenueByPlan, verificationFunnel, adBoostAdoptionRate));
    }

    private Task<int> CountDistinctActiveUsersAsync(DateTime now, TimeSpan window, CancellationToken cancellationToken)
    {
        var expiresAtCutoff = now - window + RefreshTokenLifetime;

        return _dbContext.RefreshTokens
            .Where(t => t.ExpiresAt >= expiresAtCutoff)
            .Select(t => t.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<VerificationFunnelResponse> BuildVerificationFunnelAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.SellerProfiles
            .GroupBy(s => s.VerificationStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountFor(VerificationStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        var notStarted = CountFor(VerificationStatus.NotStarted);
        var pending = CountFor(VerificationStatus.Pending);
        var manualReview = CountFor(VerificationStatus.ManualReview);
        var verified = CountFor(VerificationStatus.Verified);
        var failed = CountFor(VerificationStatus.Failed);

        var totalStarted = pending + manualReview + verified + failed;
        var verifiedRate = totalStarted == 0 ? 0m : (decimal)verified / totalStarted;
        var failedRate = totalStarted == 0 ? 0m : (decimal)failed / totalStarted;

        return new VerificationFunnelResponse(notStarted, pending, manualReview, verified, failed, verifiedRate, failedRate);
    }

    /// <summary>Of listings on a plan that permits ad-platform push, the fraction that have actually been promoted (have at least one AdCampaign row) at least once.</summary>
    private async Task<decimal> ComputeAdBoostAdoptionRateAsync(CancellationToken cancellationToken)
    {
        var pushAllowedListingIds = await _dbContext.Listings
            .Where(l => l.PromotionPlanId != null)
            .Join(
                _dbContext.PromotionPlans.Where(p => p.AdPlatformPushAllowed),
                l => l.PromotionPlanId!.Value, p => p.Id, (l, _) => l.Id)
            .ToListAsync(cancellationToken);

        if (pushAllowedListingIds.Count == 0)
        {
            return 0m;
        }

        var promotedCount = await _dbContext.AdCampaigns
            .Where(c => pushAllowedListingIds.Contains(c.ListingId))
            .Select(c => c.ListingId)
            .Distinct()
            .CountAsync(cancellationToken);

        return (decimal)promotedCount / pushAllowedListingIds.Count;
    }
}
