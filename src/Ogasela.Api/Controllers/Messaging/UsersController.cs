using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Messaging.BlockUser;

namespace Ogasela.Api.Controllers.Messaging;

[ApiController]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Blocks another user: they can no longer start a new conversation with the caller, and any further messages they send in an existing one are rejected. Does not stop the caller from messaging them.</summary>
    [HttpPost("api/v1/users/{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new BlockUserCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
