using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Moderation.ListingReview;

/// <summary>Every listing waiting for approval, oldest submission first - a first-come, first-served queue.</summary>
public sealed record GetPendingListingsQuery : IRequest<Result<IReadOnlyList<PendingListingResponse>>>;

/// <summary>FraudScore is the highest open fraud-risk score raised for the listing, if any (0-1).</summary>
public sealed record PendingListingResponse(
    Guid Id,
    string Title,
    string Description,
    decimal? Price,
    string Condition,
    IReadOnlyList<string> MediaUrls,
    string? Location,
    string? CategoryName,
    string? PromotionPlanName,
    Guid SellerProfileId,
    string SellerBusinessName,
    string SellerVerificationStatus,
    decimal? FraudScore,
    DateTime SubmittedAt);
