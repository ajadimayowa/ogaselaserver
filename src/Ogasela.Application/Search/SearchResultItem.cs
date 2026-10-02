using Ogasela.Domain.Listings;
using Ogasela.Domain.Promotions;

namespace Ogasela.Application.Search;

public sealed record SearchResultItem(
    Guid Id,
    Guid SellerId,
    Guid CategoryId,
    string Title,
    string Description,
    string? ThumbnailUrl,
    decimal? Price,
    ListingCondition Condition,
    string? Location,
    decimal? Latitude,
    decimal? Longitude,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    /// <summary>The plan this listing published under - the frontend renders its own "Featured"/"Premium" tag from this.</summary>
    string PlanTier,
    /// <summary>Distance in kilometres from the search's lat/long, or null when no coordinates were supplied on either side.</summary>
    double? DistanceKm,
    decimal Score);

public sealed record SearchResultPage(IReadOnlyList<SearchResultItem> Items, int Page, int PageSize, int TotalCount);
