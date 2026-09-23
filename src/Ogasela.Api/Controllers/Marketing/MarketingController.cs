using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Marketing;
using Ogasela.Api.RateLimiting;
using Ogasela.Application.Marketing.RequestAccountDeletion;
using Ogasela.Application.Marketing.SubmitContactForm;
using Ogasela.Application.Marketing.SubmitTesterSignup;

namespace Ogasela.Api.Controllers.Marketing;

/// <summary>
/// Public, anonymous endpoints for the ogasela.com marketing website: contact form, early-tester
/// signup, and account/data deletion requests. Every action requires a valid Cloudflare Turnstile
/// token (verified server-side) and sits behind the "marketing" rate-limit policy, since these are
/// the only anonymous write endpoints reachable from a page with no login at all.
/// </summary>
[ApiController]
[Route("api/v1/marketing")]
[EnableRateLimiting(RateLimitingExtensions.MarketingPolicy)]
public sealed class MarketingController : ControllerBase
{
    private readonly ISender _sender;

    public MarketingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Submits a Contact Us message.</summary>
    [HttpPost("contact")]
    public async Task<IActionResult> SubmitContactForm(SubmitContactFormRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitContactFormCommand(
            request.Name, request.Email, request.Subject, request.Message, request.TurnstileToken);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Joins the early-tester waitlist. Signing up twice with the same email is treated as success.</summary>
    [HttpPost("testers")]
    public async Task<IActionResult> SubmitTesterSignup(SubmitTesterSignupRequest request, CancellationToken cancellationToken)
    {
        var command = new SubmitTesterSignupCommand(request.Name, request.Email, request.TurnstileToken);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Submits an account/data deletion request. Anonymous by design - a user requesting deletion may not have (or want to use) their login.</summary>
    [HttpPost("deletion-requests")]
    public async Task<IActionResult> RequestAccountDeletion(RequestAccountDeletionRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestAccountDeletionCommand(
            request.Email, request.Phone, request.RequestType, request.Details, request.TurnstileToken);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
