using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Promotions;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Application.Promotions.UpdatePromotionPlan;

namespace Ogasela.Api.Controllers.Promotions;

/// <summary>Listing the catalog of promotion plans is public; adding plans and editing their terms (name, price, what they offer, limits) is SuperAdmin-only.</summary>
[ApiController]
public sealed class PromotionPlansController : ControllerBase
{
    private readonly ISender _sender;

    public PromotionPlansController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public - every promotion plan, cheapest first, including inactive ones.</summary>
    [HttpGet("api/v1/promotion-plans")]
    public async Task<IActionResult> GetPromotionPlans(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPromotionPlansQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Adds a plan sellers can choose when posting an ad. Only one ₦0 (free) plan may exist.</summary>
    [HttpPost("api/v1/admin/promotion-plans")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> CreatePromotionPlan(UpdatePromotionPlanRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePromotionPlanCommand(
            request.Name, request.Description, request.Features ?? [], request.DurationDays, request.PhotoLimit,
            request.VideoAllowed, request.BoostWeight, request.Price, request.AiToolTier, request.AdPlatformPushAllowed,
            request.BundledAdCreditKobo, request.IsActive);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Replaces every editable field on a plan - this is a full replace, not a partial patch.</summary>
    [HttpPut("api/v1/admin/promotion-plans/{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> UpdatePromotionPlan(
        Guid id, UpdatePromotionPlanRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePromotionPlanCommand(
            id, request.Name, request.Description, request.Features ?? [], request.DurationDays, request.PhotoLimit,
            request.VideoAllowed, request.BoostWeight, request.Price, request.AiToolTier, request.AdPlatformPushAllowed,
            request.BundledAdCreditKobo, request.IsActive);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
