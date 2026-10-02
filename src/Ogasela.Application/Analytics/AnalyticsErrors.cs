using Ogasela.Shared;

namespace Ogasela.Application.Analytics;

public static class AnalyticsErrors
{
    public static readonly Error NotASeller = new(
        "Analytics.NotASeller", "Analytics are only available to seller accounts.");

    public static readonly Error ListingNotFound = new(
        "Analytics.ListingNotFound", "That ad couldn't be found among your ads.");

    public static readonly Error SellerNotFound = new(
        "Analytics.SellerNotFound", "This seller couldn't be found.");
}
