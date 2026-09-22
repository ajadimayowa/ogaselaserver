using Microsoft.AspNetCore.Http;
using Ogasela.Domain.Listings;

namespace Ogasela.Api.Contracts.Listings;

public sealed record CreateListingRequest(
    string Title,
    string Description,
    Guid CategoryId,
    decimal? Price,
    ListingCondition Condition,
    IReadOnlyList<string>? MediaUrls,
    Guid? PromotionPlanId,
    string? Location = null,
    decimal? Latitude = null,
    decimal? Longitude = null);

public sealed record UpdateListingRequest(
    string Title,
    string Description,
    Guid CategoryId,
    decimal? Price,
    ListingCondition Condition,
    IReadOnlyList<string>? MediaUrls,
    Guid? PromotionPlanId,
    string? Location = null,
    decimal? Latitude = null,
    decimal? Longitude = null);

public sealed record UploadListingImageRequest(IFormFile Image);
