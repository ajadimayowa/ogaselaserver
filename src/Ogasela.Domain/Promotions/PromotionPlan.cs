namespace Ogasela.Domain.Promotions;

public class PromotionPlan
{
    private PromotionPlan()
    {
    }

    public Guid Id { get; private set; }

    public PromotionPlanName Name { get; private set; }

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

    public static PromotionPlan Create(
        Guid id,
        PromotionPlanName name,
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
        return new PromotionPlan
        {
            Id = id,
            Name = name,
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
}
