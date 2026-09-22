using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Application.Accounts.GetCurrentUser;
using Ogasela.Application.Accounts.SetPushToken;
using Ogasela.Application.Accounts.UpdateProfile;

namespace Ogasela.Api.Controllers.Accounts;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;

    public MeController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns the authenticated user's own profile (User.Id, role, contact details, and business name if a Seller).</summary>
    [HttpGet]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Partial update - any field left null is left unchanged. BusinessName only applies to a Seller account.</summary>
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(request.Phone, request.Email, request.BusinessName);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Registers (or clears, if Token is null) this device's push token, called on login/logout. The token is the native FCM/APNs device token, not an Expo push token - see PushNotificationChannel.</summary>
    [HttpPut("push-token")]
    public async Task<IActionResult> SetPushToken(SetPushTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetPushTokenCommand(request.Token), cancellationToken);
        return result.ToActionResult(this);
    }
}
