using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Verification;
using Ogasela.Application.Verification.CreateLivenessSession;
using Ogasela.Application.Verification.GetVerificationStatus;
using Ogasela.Application.Verification.ManualReviewDecision;
using Ogasela.Application.Verification.SubmitConsent;
using Ogasela.Application.Verification.SubmitVerification;

namespace Ogasela.Api.Controllers.Verification;

[ApiController]
[Authorize]
[Route("api/v1/verification")]
public sealed class VerificationController : ControllerBase
{
    private readonly ISender _sender;

    public VerificationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Must be accepted before submit will succeed. Records the caller's IP and the currently-configured consent version.</summary>
    [HttpPost("consent")]
    public async Task<IActionResult> SubmitConsent(CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _sender.Send(new SubmitBiometricConsentCommand(ipAddress), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Starts a liveness-check session with the face-verification provider; the returned session ref is passed back into submit.</summary>
    [HttpPost("liveness-session")]
    public async Task<IActionResult> CreateLivenessSession(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateLivenessSessionCommand(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Submits a selfie + ID photo for biometric verification (multipart/form-data, up to 20MB total). Auto-decides Verified/Failed/ManualReview by match score; ManualReview needs a Moderator decision via manual-review.</summary>
    [HttpPost("submit")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Submit([FromForm] SubmitVerificationRequest request, CancellationToken cancellationToken)
    {
        await using var selfieStream = request.Selfie.OpenReadStream();
        await using var idPhotoStream = request.IdPhoto.OpenReadStream();

        var command = new SubmitVerificationCommand(
            selfieStream,
            request.Selfie.FileName,
            request.Selfie.ContentType,
            idPhotoStream,
            request.IdPhoto.FileName,
            request.IdPhoto.ContentType,
            request.LivenessSessionRef);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The caller's own verification status and attempt history.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVerificationStatusQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Moderator-only: decides a verification record currently awaiting manual review (Decision must be Verified or Failed). id is the BiometricVerification id, not the seller id.</summary>
    [HttpPost("{id:guid}/manual-review")]
    [Authorize(Policy = "Moderator")]
    public async Task<IActionResult> ManualReview(Guid id, ManualReviewRequest request, CancellationToken cancellationToken)
    {
        var command = new ManualReviewDecisionCommand(id, request.Decision, request.Notes);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
