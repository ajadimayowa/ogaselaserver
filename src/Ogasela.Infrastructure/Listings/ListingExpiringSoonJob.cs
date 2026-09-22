using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.Events;
using Ogasela.Domain.Listings;

namespace Ogasela.Infrastructure.Listings;

/// <summary>
/// Hourly Hangfire job: any Active listing expiring within the next 24 hours is flagged
/// ExpiringSoon, and a <see cref="ListingExpiringSoonEvent"/> is raised per listing for Phase 8
/// (Notifications). Only transitions from Active (not from an already-ExpiringSoon listing),
/// so the event fires exactly once per listing.
/// </summary>
public sealed class ListingExpiringSoonJob
{
    private static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IPublisher _publisher;
    private readonly ILogger<ListingExpiringSoonJob> _logger;

    public ListingExpiringSoonJob(
        IApplicationDbContext dbContext, IDateTime dateTime, IPublisher publisher, ILogger<ListingExpiringSoonJob> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = _dateTime.UtcNow;
        var cutoff = now.Add(ExpiringSoonWindow);

        var expiringSoon = await _dbContext.Listings
            .Where(l => l.Status == ListingStatus.Active && l.ExpiresAt != null && l.ExpiresAt > now && l.ExpiresAt <= cutoff)
            .ToListAsync();

        foreach (var listing in expiringSoon)
        {
            listing.MarkExpiringSoon(now);
        }

        if (expiringSoon.Count > 0)
        {
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            foreach (var listing in expiringSoon)
            {
                await _publisher.Publish(new ListingExpiringSoonEvent(listing.Id, listing.SellerId, listing.ExpiresAt!.Value));
            }

            _logger.LogInformation("Flagged {Count} listing(s) as expiring soon", expiringSoon.Count);
        }
    }
}
