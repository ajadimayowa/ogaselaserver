using MediatR;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.GetPlatformDashboard;

public sealed record GetPlatformDashboardQuery : IRequest<Result<PlatformDashboardResponse>>;

public sealed record PlatformDashboardResponse(
    int ActiveUsersLast24h,
    int ActiveUsersLast30d,
    int ListingsCreatedLast24h,
    int ListingsCreatedLast30d,
    IReadOnlyList<PlanRevenue> RevenueByPlan,
    VerificationFunnelResponse VerificationFunnel,
    decimal AdBoostAdoptionRate);

public sealed record PlanRevenue(string Plan, decimal RevenueKobo, int PurchaseCount);

/// <summary>VerifiedRate/FailedRate are each measured against everyone who has left NotStarted (Pending + ManualReview + Verified + Failed), not against every SellerProfile ever created.</summary>
public sealed record VerificationFunnelResponse(
    int NotStarted,
    int Pending,
    int ManualReview,
    int Verified,
    int Failed,
    decimal VerifiedRate,
    decimal FailedRate);
