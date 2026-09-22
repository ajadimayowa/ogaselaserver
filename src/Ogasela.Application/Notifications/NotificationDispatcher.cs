using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.Events;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Application.Verification.Events;
using Ogasela.Domain.Notifications;

namespace Ogasela.Application.Notifications;

/// <summary>
/// Subscribes to the domain events that should notify a seller, and fans each one out to every
/// channel the recipient has enabled for that event's Category. One Notification row is written
/// per (event, enabled channel) - "no row at all" for a disabled channel is what makes that
/// case provable in a test, rather than a row that was silently never sent.
/// </summary>
public sealed class NotificationDispatcher :
    INotificationHandler<ListingExpiredEvent>,
    INotificationHandler<ListingExpiringSoonEvent>,
    INotificationHandler<VerificationDecisionEvent>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IApplicationDbContext dbContext, IDateTime dateTime, IEnumerable<INotificationChannel> channels,
        ILogger<NotificationDispatcher> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _channels = channels;
        _logger = logger;
    }

    public async Task Handle(ListingExpiredEvent notification, CancellationToken cancellationToken)
    {
        var userId = await ResolveSellerUserIdAsync(notification.SellerId, cancellationToken);
        if (userId is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new { notification.ListingId, notification.ExpiredAt });
        await DispatchAsync(userId.Value, NotificationTypes.ListingExpired, NotificationCategories.ListingLifecycle, payload, cancellationToken);
    }

    public async Task Handle(ListingExpiringSoonEvent notification, CancellationToken cancellationToken)
    {
        var userId = await ResolveSellerUserIdAsync(notification.SellerId, cancellationToken);
        if (userId is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new { notification.ListingId, notification.ExpiresAt });
        await DispatchAsync(userId.Value, NotificationTypes.ListingExpiringSoon, NotificationCategories.ListingLifecycle, payload, cancellationToken);
    }

    public async Task Handle(VerificationDecisionEvent notification, CancellationToken cancellationToken)
    {
        var userId = await ResolveSellerUserIdAsync(notification.SellerId, cancellationToken);
        if (userId is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new { notification.Decision, notification.DecidedAt });
        await DispatchAsync(userId.Value, NotificationTypes.VerificationDecision, NotificationCategories.Verification, payload, cancellationToken);
    }

    private async Task<Guid?> ResolveSellerUserIdAsync(Guid sellerProfileId, CancellationToken cancellationToken)
    {
        var sellerProfile = await _dbContext.SellerProfiles
            .Where(s => s.Id == sellerProfileId)
            .Select(s => new { s.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        return sellerProfile?.UserId;
    }

    private async Task DispatchAsync(Guid userId, string type, string category, string payload, CancellationToken cancellationToken)
    {
        foreach (var channel in _channels)
        {
            var preference = await _dbContext.NotificationPreferences.FirstOrDefaultAsync(
                p => p.UserId == userId && p.Channel == channel.Channel && p.Category == category, cancellationToken);

            // No row means the default (enabled); only an explicit disabled row suppresses this channel.
            if (preference is { Enabled: false })
            {
                continue;
            }

            var record = Notification.CreatePending(userId, channel.Channel, type, payload, _dateTime.UtcNow);
            _dbContext.Notifications.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var succeeded = await channel.SendAsync(userId, type, payload, cancellationToken);
                if (succeeded)
                {
                    record.MarkSent();
                }
                else
                {
                    record.MarkFailed();
                }
            }
            catch (Exception ex)
            {
                record.MarkFailed();
                _logger.LogWarning(
                    ex, "Failed to dispatch {Type} notification to user {UserId} over {Channel}", type, userId, channel.Channel);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
