using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Moderation.Disputes;
using Ogasela.Domain.Moderation;

namespace Ogasela.Api.Controllers.Moderation;

public sealed class RaiseDisputeRequest
{
    public Guid ListingId { get; set; }
    public Guid? ConversationId { get; set; }
    public DisputeReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<IFormFile>? Evidence { get; set; }
}

public sealed class AddDisputeMessageRequest
{
    public string Body { get; set; } = string.Empty;
    public IFormFile? Attachment { get; set; }
}

/// <summary>Buyer-seller disputes from the app. Only the two parties can see or reply to a dispute.</summary>
[ApiController]
[Authorize]
[Route("api/v1/disputes")]
public sealed class DisputesController : ControllerBase
{
    private readonly ISender _sender;

    public DisputesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>multipart/form-data: listingId, conversationId (required when the seller raises it), reason, description and up to 4 evidence files (images or PDF).</summary>
    [HttpPost]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<IActionResult> Raise([FromForm] RaiseDisputeRequest request, CancellationToken cancellationToken)
    {
        var files = request.Evidence ?? [];
        var streams = new List<Stream>();
        try
        {
            var evidence = new List<DisputeFile>();
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                evidence.Add(new DisputeFile(stream, file.FileName, file.ContentType));
            }

            var command = new RaiseDisputeCommand(request.ListingId, request.ConversationId, request.Reason, request.Description, evidence);
            return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>Disputes the caller raised or is named in, most recently created first.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyDisputesQuery(), cancellationToken)).ToActionResult(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyDisputeQuery(id), cancellationToken)).ToActionResult(this);

    /// <summary>multipart/form-data: body and an optional attachment (image or PDF, up to 10MB).</summary>
    [HttpPost("{id:guid}/messages")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> AddMessage(Guid id, [FromForm] AddDisputeMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.Attachment is null)
        {
            return (await _sender.Send(new AddDisputeMessageCommand(id, request.Body, null), cancellationToken)).ToActionResult(this);
        }

        await using var stream = request.Attachment.OpenReadStream();
        var file = new DisputeFile(stream, request.Attachment.FileName, request.Attachment.ContentType);
        return (await _sender.Send(new AddDisputeMessageCommand(id, request.Body, file), cancellationToken)).ToActionResult(this);
    }
}
