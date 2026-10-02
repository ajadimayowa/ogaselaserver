namespace Ogasela.Domain.Listings;

public class Listing
{
    /// <summary>Hard ceiling on MediaUrls, enforced regardless of the selected PromotionPlan's PhotoLimit.</summary>
    public const int MaxPhotoCount = 5;

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

    /// <summary>
    /// A moderator's note to the seller: why the last review was rejected, or why the ad was taken
    /// down. Cleared when it's resubmitted, approved or reinstated.
    /// </summary>
    public string? ReviewNote { get; private set; }

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

    /// <summary>Records the plan picked at checkout; the listing stays a Draft until it's paid for.</summary>
    public void ChoosePlan(Guid promotionPlanId, DateTime now)
    {
        PromotionPlanId = promotionPlanId;
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
        ReviewNote = null;
        UpdatedAt = publishedAt;
    }

    /// <summary>Passed every publish rule; now waits for a moderator. The plan's duration only starts once approved.</summary>
    public void SubmitForReview(DateTime now)
    {
        Status = ListingStatus.PendingReview;
        ReviewNote = null;
        UpdatedAt = now;
    }

    /// <summary>A moderator took a live ad off public view, with the reason shown to the seller.</summary>
    public void TakeDown(string reason, DateTime now)
    {
        Status = ListingStatus.Paused;
        ReviewNote = reason;
        UpdatedAt = now;
    }

    /// <summary>Back on public view after a takedown - or Expired if its time ran out meanwhile.</summary>
    public void Reinstate(DateTime now)
    {
        Status = ExpiresAt is { } expiresAt && expiresAt <= now ? ListingStatus.Expired : ListingStatus.Active;
        ReviewNote = null;
        UpdatedAt = now;
    }

    /// <summary>Moderator moves the ad to a more fitting category.</summary>
    public void ChangeCategory(Guid categoryId, DateTime now)
    {
        CategoryId = categoryId;
        UpdatedAt = now;
    }

    /// <summary>A moderator turned it down: back to Draft so the seller can fix it and submit again.</summary>
    public void RejectReview(string reason, DateTime now)
    {
        Status = ListingStatus.Draft;
        ReviewNote = reason;
        UpdatedAt = now;
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
