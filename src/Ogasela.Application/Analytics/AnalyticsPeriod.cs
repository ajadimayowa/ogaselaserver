namespace Ogasela.Application.Analytics;

/// <summary>
/// A "last N days" window, today included, plus the N days before it for the comparison delta.
/// Days are UTC calendar days, matching how the daily counters are keyed.
/// </summary>
public readonly record struct AnalyticsPeriod(DateOnly From, DateOnly To, DateOnly PreviousFrom, DateOnly PreviousTo)
{
    public static readonly int[] AllowedDays = [7, 30, 90];

    public static AnalyticsPeriod LastDays(int days, DateTime utcNow)
    {
        var to = DateOnly.FromDateTime(utcNow);
        var from = to.AddDays(-(days - 1));
        return new AnalyticsPeriod(from, to, from.AddDays(-days), from.AddDays(-1));
    }
}

/// <summary>A metric for the chosen period and the same-length period before it.</summary>
public sealed record MetricComparison(long Current, long Previous);
