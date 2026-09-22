namespace Ogasela.Application.Listings;

/// <summary>
/// Programmatic classification of why <c>PublishListingCommand</c>/<c>RepostListingCommand</c>
/// rejected a publish attempt. The API surface communicates the same information through the
/// usual <c>Result.Error.Code</c> (see <see cref="ListingErrors"/>) so clients don't need a
/// second error-modeling scheme; this enum exists for internal/analytics use where a closed
/// type is more convenient than matching on string codes.
/// </summary>
public enum PublishRejectionReason
{
    NotVerified,
    NoPlanSelected,
    CategoryNotFreeEligible,
    FreePlanCapReached,
    PaymentFailed
}
