namespace Ogasela.Application.Verification.Interfaces;

/// <summary>
/// Gate used by later phases (starting with Listings in Phase 4) to enforce that an action is
/// only available to fully biometrically verified sellers.
/// </summary>
public interface IVerificationGuard
{
    /// <summary>Throws <see cref="Domain.Verification.SellerNotVerifiedException"/> if the seller is not Verified.</summary>
    Task RequireVerifiedSellerAsync(Guid sellerId, CancellationToken cancellationToken);
}
