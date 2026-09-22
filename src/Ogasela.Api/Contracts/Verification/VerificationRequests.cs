using Microsoft.AspNetCore.Mvc;
using Ogasela.Domain.Verification;

namespace Ogasela.Api.Contracts.Verification;

/// <summary>Bound from multipart/form-data - a plain settable-property class binds IFormFile reliably, unlike a record.</summary>
public sealed class SubmitVerificationRequest
{
    [FromForm(Name = "selfie")]
    public IFormFile Selfie { get; set; } = null!;

    [FromForm(Name = "idPhoto")]
    public IFormFile IdPhoto { get; set; } = null!;

    [FromForm(Name = "livenessSessionRef")]
    public string LivenessSessionRef { get; set; } = string.Empty;
}

public sealed record ManualReviewRequest(VerificationDecision Decision, string? Notes);
