namespace Ogasela.Domain.Reviews;

/// <summary>A review between two users (typically a buyer reviewing a seller after a transaction on a listing).</summary>
public class Review
{
    private Review()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReviewerId { get; private set; }

    public Guid RevieweeId { get; private set; }

    public Guid ListingId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static Review Create(Guid reviewerId, Guid revieweeId, Guid listingId, int rating, string comment, DateTime now)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            ListingId = listingId,
            Rating = rating,
            Comment = comment,
            CreatedAt = now
        };
    }
}
