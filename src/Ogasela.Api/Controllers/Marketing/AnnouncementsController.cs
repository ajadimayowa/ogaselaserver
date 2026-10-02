using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Marketing.Announcements;

namespace Ogasela.Api.Controllers.Marketing;

/// <summary>Bound from multipart/form-data. Image is required; ctaLabel/ctaUrl are optional but come as a pair.</summary>
public sealed class CreateAnnouncementRequest
{
    [FromForm(Name = "title")]
    public string Title { get; set; } = string.Empty;

    [FromForm(Name = "caption")]
    public string Caption { get; set; } = string.Empty;

    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }

    [FromForm(Name = "ctaLabel")]
    public string? CtaLabel { get; set; }

    [FromForm(Name = "ctaUrl")]
    public string? CtaUrl { get; set; }

    [FromForm(Name = "isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Home-screen pop-up announcements: read publicly by the app, managed by SuperAdmins in the Control Portal.</summary>
[ApiController]
public sealed class AnnouncementsController : ControllerBase
{
    private readonly ISender _sender;

    public AnnouncementsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public - active announcements, newest first.</summary>
    [HttpGet("api/v1/announcements/active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetActiveAnnouncementsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("api/v1/admin/announcements")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAllAnnouncementsQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("api/v1/admin/announcements")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create([FromForm] CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (request.Image is null)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["image"] = ["An image is required."] }));
        }

        await using var image = request.Image.OpenReadStream();
        var result = await _sender.Send(
            new CreateAnnouncementCommand(
                request.Title, request.Caption, image, request.Image.FileName, request.Image.ContentType,
                request.CtaLabel, request.CtaUrl, request.IsActive),
            cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("api/v1/admin/announcements/{id:guid}/activate")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new SetAnnouncementActiveCommand(id, true), cancellationToken)).ToActionResult(this);

    [HttpPost("api/v1/admin/announcements/{id:guid}/deactivate")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new SetAnnouncementActiveCommand(id, false), cancellationToken)).ToActionResult(this);

    [HttpDelete("api/v1/admin/announcements/{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new DeleteAnnouncementCommand(id), cancellationToken)).ToActionResult(this);
}
