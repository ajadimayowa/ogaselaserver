using Ogasela.Domain.Listings;

namespace Ogasela.Api.Contracts.Search;

public sealed class SearchListingsRequest
{
    public string? Query { get; set; }

    public Guid? CategoryId { get; set; }

    public string? Location { get; set; }

    public decimal? Lat { get; set; }

    public decimal? Lng { get; set; }

    public decimal? RadiusKm { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public ListingCondition? Condition { get; set; }

    public bool VerifiedSellerOnly { get; set; }

    /// <summary>"relevance" (default), "newest", "price_asc", "price_desc", or "distance".</summary>
    public string? Sort { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
