namespace Ogasela.Domain.Analytics;

/// <summary>One row per seller per day: visits to their public seller profile (storefront) page by anyone but themselves.</summary>
public class SellerDailyStat
{
    private SellerDailyStat()
    {
    }

    public Guid SellerId { get; private set; }

    public DateOnly Date { get; private set; }

    public long ProfileVisits { get; private set; }
}
