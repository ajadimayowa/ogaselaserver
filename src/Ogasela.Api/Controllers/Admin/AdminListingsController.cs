using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Application.Admin.Listings;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Promotions;

namespace Ogasela.Api.Controllers.Admin;

public sealed record TakeDownListingRequest(string Reason);

public sealed record ChangeListingCategoryRequest(Guid CategoryId);

/// <summary>
/// The Control Portal's All Ads page: every ad in any status. Viewing needs listings.view; take
/// down / reinstate / change category need listings.manage. Approve/reject of ads awaiting approval
/// stays on AdminController (admin/listings/{id}/approve|reject).
/// </summary>
[ApiController]
[Authorize]
public sealed class AdminListingsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminListingsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>status=Active also includes ads expiring soon. categoryId includes its subcategories. reported=true keeps only ads with open reports.</summary>
    [HttpGet("api/v1/admin/listings")]
    [Authorize(Policy = "perm:listings.view")]
    public async Task<IActionResult> GetListings(
        [FromQuery] string? search, [FromQuery] ListingStatus? status, [FromQuery] Guid? categoryId,
        [FromQuery] string? plan, [FromQuery] bool reported, [FromQuery] int page, [FromQuery] int pageSize,
        CancellationToken cancellationToken) =>
        (await _sender.Send(
            new GetAdminListingsQuery(search, status, categoryId, plan, reported, page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize),
            cancellationToken)).ToActionResult(this);

    [HttpGet("api/v1/admin/listings/{id:guid}")]
    [Authorize(Policy = "perm:listings.view")]
    public async Task<IActionResult> GetListing(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAdminListingDetailQuery(id), cancellationToken)).ToActionResult(this);

    [HttpPost("api/v1/admin/listings/{id:guid}/take-down")]
    [Authorize(Policy = "perm:listings.manage")]
    public async Task<IActionResult> TakeDown(Guid id, TakeDownListingRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new TakeDownListingCommand(id, request.Reason), cancellationToken)).ToActionResult(this);

    [HttpPost("api/v1/admin/listings/{id:guid}/reinstate")]
    [Authorize(Policy = "perm:listings.manage")]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new ReinstateListingCommand(id), cancellationToken)).ToActionResult(this);

    [HttpPut("api/v1/admin/listings/{id:guid}/category")]
    [Authorize(Policy = "perm:listings.manage")]
    public async Task<IActionResult> ChangeCategory(Guid id, ChangeListingCategoryRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ChangeListingCategoryCommand(id, request.CategoryId), cancellationToken)).ToActionResult(this);
}
