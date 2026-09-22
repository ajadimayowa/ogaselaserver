namespace Ogasela.Domain.Listings;

public class Listing
{
    private Listing()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public Guid CategoryId { get; private set; }

    /// <summary>The plan this listing will publish (or last published) under. Not chosen yet on a fresh Draft.</summary>
    public Guid? PromotionPlanId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal? Price { get; private set; }

    public ListingCondition Condition { get; private set; }

    public ListingStatus Status { get; private set; }

    /// <summary>Free-text place name (e.g. a city/area), used for text-based location filtering and display.</summary>
    public string? Location { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    /// <summary>
    /// Exposed as a settable List (rather than IReadOnlyList behind a private field) so EF
    /// Core's primitive-collection JSON mapping can materialize it directly on load.
    /// </summary>
    public List<string> MediaUrls { get; private set; } = new();

    public DateTime? PublishedAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Listing CreateDraft(
        Guid id,
        Guid sellerId,
        Guid categoryId,
        Guid? promotionPlanId,
        string title,
        string description,
        decimal? price,
        ListingCondition condition,
        IEnumerable<string> mediaUrls,
        DateTime now)
    {
        var listing = new Listing
        {
            Id = id,
            SellerId = sellerId,
            CategoryId = categoryId,
            PromotionPlanId = promotionPlanId,
            Title = title,
            Description = description,
            Price = price,
            Condition = condition,
            Status = ListingStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        listing.MediaUrls.AddRange(mediaUrls);
        return listing;
    }

    public void UpdateDetails(
        Guid categoryId,
        Guid? promotionPlanId,
        string title,
        string description,
        decimal? price,
        ListingCondition condition,
        IEnumerable<string> mediaUrls,
        DateTime now)
    {
        CategoryId = categoryId;
        PromotionPlanId = promotionPlanId;
        Title = title;
        Description = description;
        Price = price;
        Condition = condition;

        MediaUrls.Clear();
        MediaUrls.AddRange(mediaUrls);

        UpdatedAt = now;
    }

    public void SetLocation(string? location, decimal? latitude, decimal? longitude, DateTime now)
    {
        Location = location;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = now;
    }

    public void Publish(DateTime publishedAt, DateTime expiresAt)
    {
        Status = ListingStatus.Active;
        PublishedAt = publishedAt;
        ExpiresAt = expiresAt;
        UpdatedAt = publishedAt;
    }

    public void Pause(DateTime now)
    {
        Status = ListingStatus.Paused;
        UpdatedAt = now;
    }

    public void MarkSold(DateTime now)
    {
        Status = ListingStatus.Sold;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTime now)
    {
        Status = ListingStatus.Expired;
        UpdatedAt = now;
    }

    public void MarkExpiringSoon(DateTime now)
    {
        Status = ListingStatus.ExpiringSoon;
        UpdatedAt = now;
    }
}
