namespace Ogasela.Domain.Reviews;

/// <summary>
/// One row per seller (SellerId is the primary key - there's exactly one current score per
/// seller, recomputed in place nightly by RecomputeTrustScoresJob), not a history table.
/// </summary>
public class TrustScore
{
    private TrustScore()
    {
    }

    public Guid SellerId { get; private set; }

    public decimal Score { get; private set; }

    public DateTime LastComputedAt { get; private set; }

    public static TrustScore Create(Guid sellerId, decimal score, DateTime now) => new()
    {
        SellerId = sellerId,
        Score = Clamp(score),
        LastComputedAt = now
    };

    public void Update(decimal score, DateTime now)
    {
        Score = Clamp(score);
        LastComputedAt = now;
    }

    private static decimal Clamp(decimal score) => Math.Clamp(score, 0m, 100m);
}
