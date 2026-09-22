using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Payments.HandlePaymentWebhook;
using Ogasela.Infrastructure.Payments;

namespace Ogasela.Api.Controllers.Payments;

/// <summary>
/// No JWT auth on either route - these are called by Paystack/Flutterwave's own servers, not
/// a logged-in client. Authenticity is instead established by verifying each gateway's
/// signature inside <c>HandlePaymentWebhookCommand</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly ISender _sender;

    public WebhooksController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Called by Paystack, not a client. Verifies the x-paystack-signature HMAC against the raw body before touching anything; safe to receive the same event more than once.</summary>
    [HttpPost("paystack")]
    public async Task<IActionResult> Paystack(CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

        var result = await _sender.Send(
            new HandlePaymentWebhookCommand(PaystackGateway.Name, rawBody, signature), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Called by Flutterwave, not a client. Verifies the verif-hash header against the pre-shared secret before touching anything; safe to receive the same event more than once.</summary>
    [HttpPost("flutterwave")]
    public async Task<IActionResult> Flutterwave(CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync();
        var signature = Request.Headers["verif-hash"].FirstOrDefault();

        var result = await _sender.Send(
            new HandlePaymentWebhookCommand(FlutterwaveGateway.Name, rawBody, signature), cancellationToken);
        return result.ToActionResult(this);
    }

    private async Task<string> ReadRawBodyAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
