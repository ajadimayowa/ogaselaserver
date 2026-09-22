namespace Ogasela.Domain.Verification;

/// <summary>
/// Records a seller's explicit acceptance of biometric data processing. This is intentionally
/// separate from acceptance of the general Terms of Service - NDPA processing of biometric
/// data requires its own, distinct, informed consent.
/// </summary>
public class BiometricConsent
{
    private BiometricConsent()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public string ConsentVersion { get; private set; } = string.Empty;

    public DateTime AcceptedAt { get; private set; }

    public string IpAddress { get; private set; } = string.Empty;

    public static BiometricConsent Create(Guid sellerId, string consentVersion, DateTime acceptedAt, string ipAddress)
    {
        return new BiometricConsent
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            ConsentVersion = consentVersion,
            AcceptedAt = acceptedAt,
            IpAddress = ipAddress
        };
    }
}
