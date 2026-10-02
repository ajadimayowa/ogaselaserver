using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ogasela.Application.Analytics;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Infrastructure.Persistence;

namespace Ogasela.Infrastructure.Analytics;

/// <summary>
/// Increments counters with INSERT ... ON CONFLICT DO UPDATE, so concurrent requests add up
/// correctly without a read-modify-write race. Errors are logged and swallowed (see the interface).
/// </summary>
public sealed class EngagementTracker : IEngagementTracker
{
    private readonly OgaselaDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ILogger<EngagementTracker> _logger;

    public EngagementTracker(OgaselaDbContext dbContext, IDateTime dateTime, ILogger<EngagementTracker> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _logger = logger;
    }

    private static string ColumnFor(ListingEngagement engagement) => engagement switch
    {
        ListingEngagement.Impression => "Impressions",
        ListingEngagement.View => "Views",
        ListingEngagement.CallClick => "CallClicks",
        ListingEngagement.MessageStart => "MessageStarts",
        ListingEngagement.Save => "Saves",
        _ => throw new ArgumentOutOfRangeException(nameof(engagement))
    };

    public async Task RecordAsync(
        IReadOnlyCollection<(Guid ListingId, Guid SellerId)> listings, ListingEngagement engagement, CancellationToken cancellationToken)
    {
        if (listings.Count == 0)
        {
            return;
        }

        try
        {
            // Column name comes from the fixed switch above, never from input.
            var column = ColumnFor(engagement);
            var sql = $"""
                INSERT INTO "ListingDailyStats" ("ListingId", "SellerId", "Date", "Impressions", "Views", "CallClicks", "MessageStarts", "Saves")
                SELECT x.listing_id, x.seller_id, @date, 0, 0, 0, 0, 0
                FROM unnest(@listingIds, @sellerIds) AS x(listing_id, seller_id)
                ON CONFLICT ("ListingId", "Date") DO NOTHING;
                UPDATE "ListingDailyStats" SET "{column}" = "{column}" + 1
                WHERE "Date" = @date AND "ListingId" = ANY(@listingIds);
                """;

            var distinct = listings.DistinctBy(l => l.ListingId).ToArray();
            await _dbContext.Database.ExecuteSqlRawAsync(
                sql,
                [
                    new NpgsqlParameter("date", DateOnly.FromDateTime(_dateTime.UtcNow)),
                    new NpgsqlParameter("listingIds", distinct.Select(l => l.ListingId).ToArray()),
                    new NpgsqlParameter("sellerIds", distinct.Select(l => l.SellerId).ToArray())
                ],
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record {Engagement} for {Count} listing(s)", engagement, listings.Count);
        }
    }

    public async Task RecordProfileVisitAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "SellerDailyStats" ("SellerId", "Date", "ProfileVisits")
                VALUES (@sellerId, @date, 1)
                ON CONFLICT ("SellerId", "Date") DO UPDATE SET "ProfileVisits" = "SellerDailyStats"."ProfileVisits" + 1;
                """,
                [
                    new NpgsqlParameter("sellerId", sellerId),
                    new NpgsqlParameter("date", DateOnly.FromDateTime(_dateTime.UtcNow))
                ],
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record a profile visit for seller {SellerId}", sellerId);
        }
    }
}
