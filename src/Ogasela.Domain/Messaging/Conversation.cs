namespace Ogasela.Domain.Messaging;

/// <summary>
/// A buyer/seller conversation anchored to a specific listing. BuyerId and SellerId are both
/// User ids (not SellerProfile ids like Listing.SellerId) - a buyer has no SellerProfile, so
/// messaging has to operate on the User identity both parties actually share.
/// </summary>
public class Conversation
{
    private Conversation()
    {
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid BuyerId { get; private set; }

    public Guid SellerId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastMessageAt { get; private set; }

    public static Conversation Start(Guid listingId, Guid buyerId, Guid sellerId, DateTime now)
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            BuyerId = buyerId,
            SellerId = sellerId,
            CreatedAt = now,
            LastMessageAt = now
        };
    }

    public bool HasParticipant(Guid userId) => BuyerId == userId || SellerId == userId;

    public Guid GetOtherParticipant(Guid userId) => userId == BuyerId ? SellerId : BuyerId;

    public void TouchLastMessageAt(DateTime now) => LastMessageAt = now;
}
