using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.AdIntegrations;
using Ogasela.Application.AdIntegrations.EndCampaign;
using Ogasela.Application.AdIntegrations.GetAdPerformance;
using Ogasela.Application.AdIntegrations.PauseCampaign;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Application.AdIntegrations.ResumeCampaign;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Api.Controllers.AdIntegrations;

/// <summary>
/// Pushes a listing to Facebook/TikTok ads and manages the resulting campaign. Requires the
/// seller to already have connected the given platform (see AdIntegrationsController) and be on
/// a PromotionPlan with AdPlatformPushAllowed. Pause/resume/end act on the most recently created
/// campaign for this listing+platform pair, not a specific campaign id.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/listings/{id:guid}")]
public sealed class ListingAdsController : ControllerBase
{
    private readonly ISender _sender;

    public ListingAdsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Creates a new ad campaign for this listing on the given platform. If AdCopyTitle/AdCopyDescription aren't supplied, AI-generated copy is attempted (when the seller's plan includes it) before falling back to the listing's own title/description.</summary>
    [HttpPost("promote/{platform}")]
    public async Task<IActionResult> Promote(Guid id, AdPlatform platform, PromoteListingRequest request, CancellationToken cancellationToken)
    {
        var command = new PromoteListingCommand(
            id, platform, request.BudgetKobo, request.DurationDays, request.Audience, request.AdCopyTitle, request.AdCopyDescription);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Aggregated impressions/clicks/spend across every platform this listing has ever been promoted on (most recent campaign per platform only).</summary>
    [HttpGet("ad-performance")]
    public async Task<IActionResult> GetAdPerformance(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdPerformanceQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Pauses the listing's active campaign on this platform. No-op target: a campaign already Ended returns AdIntegrations.CampaignNotFound.</summary>
    [HttpPost("promote/{platform}/pause")]
    public async Task<IActionResult> Pause(Guid id, AdPlatform platform, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PauseCampaignCommand(id, platform), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Resumes a previously-paused campaign on this platform.</summary>
    [HttpPost("promote/{platform}/resume")]
    public async Task<IActionResult> Resume(Guid id, AdPlatform platform, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ResumeCampaignCommand(id, platform), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Ends the campaign on this platform permanently - use pause instead if it might be resumed later.</summary>
    [HttpPost("promote/{platform}/end")]
    public async Task<IActionResult> End(Guid id, AdPlatform platform, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new EndCampaignCommand(id, platform), cancellationToken);
        return result.ToActionResult(this);
    }
}
