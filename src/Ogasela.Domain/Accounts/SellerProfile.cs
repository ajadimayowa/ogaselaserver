namespace Ogasela.Domain.Accounts;

public class SellerProfile
{
    private SellerProfile()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string BusinessName { get; private set; } = string.Empty;

    public string? RcNumber { get; private set; }

    public string? Nin { get; private set; }

    /// <summary>Where buyers can find the seller's shop, shown on listing detail. Optional - sellers without a physical shop leave it empty.</summary>
    public string? StoreAddress { get; private set; }

    public VerificationStatus VerificationStatus { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static SellerProfile Create(Guid userId, string businessName, string? rcNumber, string? nin)
    {
        return new SellerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = businessName,
            RcNumber = rcNumber,
            Nin = nin,
            VerificationStatus = VerificationStatus.NotStarted,
            VerifiedAt = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateBusinessName(string businessName)
    {
        BusinessName = businessName;
    }

    public void UpdateStoreAddress(string? storeAddress)
    {
        StoreAddress = string.IsNullOrWhiteSpace(storeAddress) ? null : storeAddress.Trim();
    }

    public void UpdateVerificationStatus(VerificationStatus status, DateTime? decidedAt)
    {
        VerificationStatus = status;

        if (status == VerificationStatus.Verified)
        {
            VerifiedAt = decidedAt;
        }
    }

    /// <summary>NDPA data-subject deletion: clears identifying business/ID fields. Id/UserId/VerificationStatus/CreatedAt are kept, since Listings/Transactions/TrustScore still reference this SellerId for financial/audit history.</summary>
    public void Anonymize()
    {
        BusinessName = "Erased Seller";
        RcNumber = null;
        Nin = null;
        StoreAddress = null;
    }
}
