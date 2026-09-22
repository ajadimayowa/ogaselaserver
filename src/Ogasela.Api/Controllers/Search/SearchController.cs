using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Search;
using Ogasela.Application.Search;

namespace Ogasela.Api.Controllers.Search;

[ApiController]
public sealed class SearchController : ControllerBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Full-text + filtered listing search (Postgres tsvector). Sort defaults to relevance; pass sort=newest|price_asc|price_desc|distance (distance requires Lat/Lng). PageSize is capped at 100.</summary>
    [HttpGet("api/v1/search")]
    public async Task<IActionResult> Search([FromQuery] SearchListingsRequest request, CancellationToken cancellationToken)
    {
        var criteria = new SearchQuery(
            request.Query,
            request.CategoryId,
            request.Location,
            request.Lat,
            request.Lng,
            request.RadiusKm,
            request.MinPrice,
            request.MaxPrice,
            request.Condition,
            request.VerifiedSellerOnly,
            ParseSort(request.Sort),
            request.Page <= 0 ? 1 : request.Page,
            request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100));

        var result = await _sender.Send(new SearchListingsQuery(criteria), cancellationToken);
        return result.ToActionResult(this);
    }

    private static SearchSortOption ParseSort(string? sort) => sort?.Trim().ToLowerInvariant() switch
    {
        "newest" => SearchSortOption.Newest,
        "price_asc" => SearchSortOption.PriceAsc,
        "price_desc" => SearchSortOption.PriceDesc,
        "distance" => SearchSortOption.Distance,
        _ => SearchSortOption.Relevance
    };
}
