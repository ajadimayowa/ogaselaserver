using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Analytics;
using Ogasela.Application.Analytics.GetListingAnalytics;
using Ogasela.Application.Analytics.GetSellerAnalytics;
using Ogasela.Application.Analytics.GetSellerProfile;
using Ogasela.Application.Analytics.RecordEngagement;

namespace Ogasela.Api.Controllers.Analytics;

public sealed record RecordEngagementRequest(ListingEngagement Engagement);

/// <summary>
/// Seller analytics plus the two things that feed it from outside the server: app-side engagement
/// (Call taps, saves) and seller profile visits. Impressions, views and new chats are counted
/// where they already happen (search, listing detail, start conversation).
/// </summary>
[ApiController]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ISender _sender;

    public AnalyticsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Records a Call tap or a save on someone else's live ad. Engagement must be CallClick or Save.</summary>
    [HttpPost("api/v1/listings/{id:guid}/engagements")]
    [Authorize]
    public async Task<IActionResult> RecordEngagement(Guid id, RecordEngagementRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RecordListingEngagementCommand(id, request.Engagement), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Public - a seller's storefront header (business, rating, live ad count). Counts as a profile visit unless it's the seller's own.</summary>
    [HttpGet("api/v1/sellers/{id:guid}")]
    public async Task<IActionResult> GetSellerProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSellerProfileQuery(id), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The signed-in seller's totals for the last 7, 30 or 90 days, each with the previous period.</summary>
    [HttpGet("api/v1/me/analytics")]
    [Authorize]
    public async Task<IActionResult> GetSellerAnalytics([FromQuery] int days, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSellerAnalyticsQuery(days == 0 ? 30 : days), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>One of the signed-in seller's ads: Ogasela engagement for the period plus Facebook/TikTok campaign totals.</summary>
    [HttpGet("api/v1/me/analytics/listings/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetListingAnalytics(Guid id, [FromQuery] int days, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetListingAnalyticsQuery(id, days == 0 ? 30 : days), cancellationToken);
        return result.ToActionResult(this);
    }
}
