using MediatR;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.PromoteListing;

/// <summary>
/// AdCopyTitle/AdCopyDescription are optional - when the seller doesn't supply their own, the
/// handler tries to auto-generate copy via Phase 9's IListingCopyGenerator (gated by
/// AiAccessGuard on AiCapability.AdCopyAssist). That gate is a soft fallback, not a second hard
/// rejection alongside AdPlatformPushAllowed: a seller whose plan doesn't include AI ad copy (or
/// whose generation call fails) still gets to promote, just with the listing's own title/description.
/// </summary>
public sealed record PromoteListingCommand(
    Guid ListingId,
    AdPlatform Platform,
    decimal BudgetKobo,
    int DurationDays,
    string? Audience,
    string? AdCopyTitle,
    string? AdCopyDescription) : IRequest<Result<AdCampaignResponse>>;

public sealed record AdCampaignResponse(
    Guid Id,
    Guid ListingId,
    AdPlatform Platform,
    string ExternalCampaignId,
    decimal BudgetKobo,
    int DurationDays,
    AdCampaignStatus Status,
    DateTime? LastMetricsSyncAt,
    DateTime CreatedAt)
{
    public static AdCampaignResponse From(AdCampaign campaign) => new(
        campaign.Id, campaign.ListingId, campaign.Platform, campaign.ExternalCampaignId,
        campaign.BudgetKobo, campaign.DurationDays, campaign.Status, campaign.LastMetricsSyncAt, campaign.CreatedAt);
}
