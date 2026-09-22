using Ogasela.Shared;

namespace Ogasela.Application.Reviews;

public static class ReviewErrors
{
    public static readonly Error ListingNotFound = new(
        "Review.ListingNotFound", "The listing could not be found.");

    public static readonly Error CannotReviewSelf = new(
        "Review.CannotReviewSelf", "You cannot review yourself.");

    public static readonly Error AlreadyReviewed = new(
        "Review.AlreadyReviewed", "You have already reviewed this listing.");

    public static readonly Error SellerProfileNotFound = new(
        "Review.SellerProfileNotFound", "No seller profile was found for this seller.");
}
