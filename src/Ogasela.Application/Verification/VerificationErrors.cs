using Ogasela.Shared;

namespace Ogasela.Application.Verification;

public static class VerificationErrors
{
    public static readonly Error SellerProfileNotFound = new(
        "Verification.SellerProfileNotFound", "No seller profile was found for the current user.");

    public static readonly Error ConsentRequired = new(
        "Verification.ConsentRequired", "Biometric consent must be accepted before submitting verification.");

    public static readonly Error AlreadyVerified = new(
        "Verification.AlreadyVerified", "This seller is already verified.");

    public static readonly Error PendingManualReview = new(
        "Verification.PendingManualReview", "A previous submission is awaiting manual review. Wait for that decision before submitting again.");

    public static readonly Error VerificationNotFound = new(
        "Verification.NotFound", "The verification record could not be found.");

    public static readonly Error NotPendingManualReview = new(
        "Verification.NotPendingManualReview", "This verification record is not awaiting manual review.");

    public static readonly Error RawImagesUnavailable = new(
        "Verification.RawImagesUnavailable", "The raw images for this submission have already expired and can no longer be enrolled. Ask the seller to resubmit.");

    public static readonly Error InvalidManualDecision = new(
        "Verification.InvalidManualDecision", "A manual review decision must be either Verified or Failed.");
}
