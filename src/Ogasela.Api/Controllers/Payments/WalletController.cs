using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Payments;
using Ogasela.Application.Payments.FundWallet;
using Ogasela.Application.Payments.GetTransactions;
using Ogasela.Application.Payments.GetWallet;

namespace Ogasela.Api.Controllers.Payments;

[ApiController]
[Authorize]
public sealed class WalletController : ControllerBase
{
    private readonly ISender _sender;

    public WalletController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Starts a wallet top-up: creates a Pending Transaction and returns the active payment gateway's checkout redirect URL. The wallet is credited only once the gateway's webhook confirms payment.</summary>
    [HttpPost("api/v1/wallet/fund")]
    public async Task<IActionResult> Fund(FundWalletRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new FundWalletCommand(request.AmountKobo), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The caller's current wallet balance (kobo).</summary>
    [HttpGet("api/v1/wallet")]
    public async Task<IActionResult> GetWallet(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWalletQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>The caller's wallet transaction history (top-ups, plan purchases, refunds), newest first.</summary>
    [HttpGet("api/v1/transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var query = new GetTransactionsQuery(page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
