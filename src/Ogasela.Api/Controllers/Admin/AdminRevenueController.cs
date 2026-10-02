using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Admin.Revenue;
using Ogasela.Domain.Payments;

namespace Ogasela.Api.Controllers.Admin;

/// <summary>The Control Portal's Revenue page - SuperAdmin only. from/to are UTC dates (yyyy-MM-dd), both inclusive.</summary>
[ApiController]
[Authorize(Policy = "SuperAdmin")]
[Route("api/v1/admin/revenue")]
public sealed class AdminRevenueController : ControllerBase
{
    private readonly ISender _sender;

    public AdminRevenueController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetRevenueSummaryQuery(from, to), cancellationToken)).ToActionResult(this);

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionType? type, [FromQuery] TransactionStatus? status, [FromQuery] string? search,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page, [FromQuery] int pageSize,
        CancellationToken cancellationToken) =>
        (await _sender.Send(
            new GetRevenueTransactionsQuery(type, status, search, from, to, page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize),
            cancellationToken)).ToActionResult(this);
}
