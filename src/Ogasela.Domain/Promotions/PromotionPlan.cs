namespace Ogasela.Domain.Promotions;

public class PromotionPlan
{
    private PromotionPlan()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>A one-line pitch shown to sellers when they choose a plan.</summary>
    public string? Description { get; private set; }

    /// <summary>What the plan offers, as short bullet points shown to sellers (e.g. "Top of search results").</summary>
    public List<string> Features { get; private set; } = [];

    public int DurationDays { get; private set; }

    public int PhotoLimit { get; private set; }

    public bool VideoAllowed { get; private set; }

    /// <summary>Used later by Search ranking to boost listings under this plan.</summary>
    public int BoostWeight { get; private set; }

    /// <summary>Price in kobo (1/100 Naira) to avoid floating-point currency errors.</summary>
    public decimal Price { get; private set; }

    public AiToolTier AiToolTier { get; private set; }

    public bool AdPlatformPushAllowed { get; private set; }

    public int BundledAdCreditKobo { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>A plan that costs nothing - the Free-plan rules (category eligibility, active-ad cap) apply to it, and publishing never charges.</summary>
    public bool IsFree => Price == 0m;

    public static PromotionPlan Create(
        Guid id,
        string name,
        int durationDays,
        int photoLimit,
        bool videoAllowed,
        int boostWeight,
        decimal price,
        AiToolTier aiToolTier,
        bool adPlatformPushAllowed,
        int bundledAdCreditKobo,
        bool isActive,
        string? description = null,
        IEnumerable<string>? features = null)
    {
        return new PromotionPlan
        {
            Id = id,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Features = CleanFeatures(features),
            DurationDays = durationDays,
            PhotoLimit = photoLimit,
            VideoAllowed = videoAllowed,
            BoostWeight = boostWeight,
            Price = price,
            AiToolTier = aiToolTier,
            AdPlatformPushAllowed = adPlatformPushAllowed,
            BundledAdCreditKobo = bundledAdCreditKobo,
            IsActive = isActive
        };
    }

    public void Update(
        string name,
        string? description,
        IEnumerable<string> features,
        int durationDays,
        int photoLimit,
        bool videoAllowed,
        int boostWeight,
        decimal price,
        AiToolTier aiToolTier,
        bool adPlatformPushAllowed,
        int bundledAdCreditKobo,
        bool isActive)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Features = CleanFeatures(features);
        DurationDays = durationDays;
        PhotoLimit = photoLimit;
        VideoAllowed = videoAllowed;
        BoostWeight = boostWeight;
        Price = price;
        AiToolTier = aiToolTier;
        AdPlatformPushAllowed = adPlatformPushAllowed;
        BundledAdCreditKobo = bundledAdCreditKobo;
        IsActive = isActive;
    }

    private static List<string> CleanFeatures(IEnumerable<string>? features) =>
        (features ?? []).Select(f => f.Trim()).Where(f => f.Length > 0).Distinct().ToList();
}
