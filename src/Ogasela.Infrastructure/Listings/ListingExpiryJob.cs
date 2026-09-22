using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.Events;
using Ogasela.Domain.Listings;

namespace Ogasela.Infrastructure.Listings;

/// <summary>
/// Hourly Hangfire job: any Active or ExpiringSoon listing whose ExpiresAt has passed becomes
/// Expired, and a <see cref="ListingExpiredEvent"/> is raised per listing for Phase 8
/// (Notifications) to eventually subscribe to.
/// </summary>
public sealed class ListingExpiryJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IPublisher _publisher;
    private readonly ILogger<ListingExpiryJob> _logger;

    public ListingExpiryJob(
        IApplicationDbContext dbContext, IDateTime dateTime, IPublisher publisher, ILogger<ListingExpiryJob> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = _dateTime.UtcNow;

        var expiring = await _dbContext.Listings
            .Where(l => (l.Status == ListingStatus.Active || l.Status == ListingStatus.ExpiringSoon)
                        && l.ExpiresAt != null && l.ExpiresAt <= now)
            .ToListAsync();

        foreach (var listing in expiring)
        {
            listing.MarkExpired(now);
        }

        if (expiring.Count > 0)
        {
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            foreach (var listing in expiring)
            {
                await _publisher.Publish(new ListingExpiredEvent(listing.Id, listing.SellerId, now));
            }

            _logger.LogInformation("Expired {Count} listing(s)", expiring.Count);
        }
    }
}
