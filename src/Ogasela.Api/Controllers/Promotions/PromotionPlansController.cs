using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Promotions;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Application.Promotions.UpdatePromotionPlan;

namespace Ogasela.Api.Controllers.Promotions;

/// <summary>Listing the catalog of promotion plans is public; editing a plan's terms (pricing, photo limits, AI tier, ad-push eligibility, etc.) is SuperAdmin-only.</summary>
[ApiController]
public sealed class PromotionPlansController : ControllerBase
{
    private readonly ISender _sender;

    public PromotionPlansController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public - every promotion plan (Free/Basic/Standard/Premium), including inactive ones.</summary>
    [HttpGet("api/v1/promotion-plans")]
    public async Task<IActionResult> GetPromotionPlans(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPromotionPlansQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Replaces every editable field on a plan - this is a full replace, not a partial patch.</summary>
    [HttpPut("api/v1/admin/promotion-plans/{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> UpdatePromotionPlan(
        Guid id, UpdatePromotionPlanRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePromotionPlanCommand(
            id,
            request.DurationDays,
            request.PhotoLimit,
            request.VideoAllowed,
            request.BoostWeight,
            request.Price,
            request.AiToolTier,
            request.AdPlatformPushAllowed,
            request.BundledAdCreditKobo,
            request.IsActive);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
