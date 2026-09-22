using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.AdIntegrations.ConnectAdAccount;
using Ogasela.Application.AdIntegrations.HandleOAuthCallback;
using Ogasela.Application.AdIntegrations.RevokeAdAccount;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Api.Controllers.AdIntegrations;

/// <summary>Manages a seller's own OAuth connection to Facebook or TikTok's ad platform - connecting, the platform's OAuth callback, and revoking. See ListingAdsController for actually promoting a listing once connected.</summary>
[ApiController]
[Route("api/v1/integrations/{platform}")]
public sealed class AdIntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    public AdIntegrationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Starts the OAuth flow: returns the platform's own authorization URL for the client to redirect the seller to.</summary>
    [Authorize]
    [HttpPost("connect")]
    public async Task<IActionResult> Connect(AdPlatform platform, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConnectAdAccountCommand(platform), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// No JWT auth - this is the platform's own redirect back after the seller approves access,
    /// which carries no bearer token. "state" (the sellerId that started the flow, round-tripped
    /// through the platform - see IAdPlatformClient.GetOAuthUrlAsync) is what ties it back to a seller.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        AdPlatform platform, [FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new HandleOAuthCallbackCommand(platform, code, state), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Revokes the seller's currently-active connection for this platform (best-effort against the platform itself). Any AdCampaign already running is left as-is on the platform side - pause or end it first if it should stop.</summary>
    [Authorize]
    [HttpDelete("")]
    public async Task<IActionResult> Revoke(AdPlatform platform, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeAdAccountCommand(platform), cancellationToken);
        return result.ToActionResult(this);
    }
}
