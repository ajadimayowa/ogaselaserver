namespace Ogasela.Domain.Verification;

/// <summary>
/// Thrown by <c>IVerificationGuard.RequireVerifiedSellerAsync</c> when an action that requires
/// a fully verified seller (e.g. publishing a listing, from Phase 4 onward) is attempted by a
/// seller whose <see cref="Accounts.SellerProfile.VerificationStatus"/> is not
/// <see cref="Accounts.VerificationStatus.Verified"/>.
/// </summary>
public sealed class SellerNotVerifiedException : Exception
{
    public SellerNotVerifiedException(Guid sellerId)
        : base($"Seller '{sellerId}' has not completed biometric verification.")
    {
        SellerId = sellerId;
    }

    public Guid SellerId { get; }
}
