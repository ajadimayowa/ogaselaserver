using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Payments.RefundTransaction;

namespace Ogasela.Api.Controllers.Payments;

/// <summary>FinanceAdmin-only wallet operations support.</summary>
[ApiController]
[Authorize(Policy = "FinanceAdmin")]
public sealed class AdminTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminTransactionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Reverses a successful Transaction and credits the amount back to the seller's wallet. Only a Success transaction can be refunded; this does not call back out to the original payment gateway.</summary>
    [HttpPost("api/v1/admin/transactions/{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefundTransactionCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
