using Microsoft.AspNetCore.Http;
using Ogasela.Domain.Listings;

namespace Ogasela.Api.Contracts.Listings;

/// <summary>MediaUrls: at most Listing.MaxPhotoCount (5) URLs, each returned by POST api/v1/listings/images.</summary>
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

/// <summary>MediaUrls: at most Listing.MaxPhotoCount (5) URLs, each returned by POST api/v1/listings/images.</summary>
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

public sealed record ListingCheckoutRequest(Guid PromotionPlanId, Ogasela.Application.Listings.Checkout.ListingPaymentMethod PaymentMethod);
