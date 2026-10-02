using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.Events;
using Ogasela.Application.Messaging;
using Ogasela.Application.Reviews;
using Ogasela.Application.Verification.Events;
using Ogasela.Domain.Notifications;
using Ogasela.Domain.Verification;

namespace Ogasela.Application.Notifications.Inbox;

/// <summary>
/// Turns domain events into entries in the recipient's in-app inbox (the bell). Runs alongside
/// NotificationDispatcher (push/email), independent of channel preferences - the inbox is the
/// record of what happened. Never throws: a failed inbox write mustn't fail the action behind it.
/// </summary>
public sealed class InboxNotificationWriter :
    INotificationHandler<ListingReviewedEvent>,
    INotificationHandler<ListingExpiringSoonEvent>,
    INotificationHandler<ListingExpiredEvent>,
    INotificationHandler<VerificationDecisionEvent>,
    INotificationHandler<NewMessageEvent>,
    INotificationHandler<ReviewReceivedEvent>
{
    private const int PreviewLength = 120;

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ILogger<InboxNotificationWriter> _logger;

    public InboxNotificationWriter(IApplicationDbContext dbContext, IDateTime dateTime, ILogger<InboxNotificationWriter> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _logger = logger;
    }

    public Task Handle(ListingReviewedEvent e, CancellationToken cancellationToken) =>
        WriteForSellerAsync(e.SellerId, cancellationToken, userId => e.Approved
            ? InboxNotification.Create(userId, InboxNotificationTypes.AdApproved, "Your ad is live",
                $"“{e.Title}” was approved and buyers can now see it.", e.ListingId, null, _dateTime.UtcNow)
            : InboxNotification.Create(userId, InboxNotificationTypes.AdRejected, "Your ad wasn't approved",
                Truncate($"“{e.Title}”: {e.Reason}"), e.ListingId, null, _dateTime.UtcNow));

    public async Task Handle(ListingExpiringSoonEvent e, CancellationToken cancellationToken)
    {
        var title = await ListingTitleAsync(e.ListingId, cancellationToken);
        await WriteForSellerAsync(e.SellerId, cancellationToken, userId => InboxNotification.Create(
            userId, InboxNotificationTypes.AdExpiringSoon, "Your ad expires soon",
            $"“{title}” expires on {e.ExpiresAt:d MMM}. Repost it to keep it live.", e.ListingId, null, _dateTime.UtcNow));
    }

    public async Task Handle(ListingExpiredEvent e, CancellationToken cancellationToken)
    {
        var title = await ListingTitleAsync(e.ListingId, cancellationToken);
        await WriteForSellerAsync(e.SellerId, cancellationToken, userId => InboxNotification.Create(
            userId, InboxNotificationTypes.AdExpired, "Your ad has expired",
            $"“{title}” is no longer visible to buyers. Repost it to keep selling.", e.ListingId, null, _dateTime.UtcNow));
    }

    public Task Handle(VerificationDecisionEvent e, CancellationToken cancellationToken) =>
        WriteForSellerAsync(e.SellerId, cancellationToken, userId =>
        {
            var (title, body) = e.Decision switch
            {
                VerificationDecision.Verified => ("You're verified", "Your identity check passed - you can now publish ads."),
                VerificationDecision.ManualReview => ("Verification under review", "Our team is reviewing your identity check. We'll let you know soon."),
                _ => ("Verification unsuccessful", "Your identity check didn't pass. You can try again from your profile.")
            };
            return InboxNotification.Create(userId, InboxNotificationTypes.Verification, title, body, null, null, _dateTime.UtcNow);
        });

    public async Task Handle(NewMessageEvent e, CancellationToken cancellationToken)
    {
        try
        {
            var senderName = await _dbContext.Users
                .Where(u => u.Id == e.SenderId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Someone";
            var now = _dateTime.UtcNow;

            // While the last message notification for this chat is unread, fold new messages into it.
            var existing = await _dbContext.InboxNotifications.FirstOrDefaultAsync(
                n => n.UserId == e.RecipientId && n.ConversationId == e.ConversationId
                     && n.Type == InboxNotificationTypes.NewMessage && n.ReadAt == null,
                cancellationToken);

            if (existing is not null)
            {
                existing.Bump($"{existing.Count + 1} new messages from {senderName}", Truncate(e.Preview), now);
            }
            else
            {
                _dbContext.InboxNotifications.Add(InboxNotification.Create(
                    e.RecipientId, InboxNotificationTypes.NewMessage, $"New message from {senderName}",
                    Truncate(e.Preview), e.ListingId, e.ConversationId, now));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write a message inbox notification for conversation {ConversationId}", e.ConversationId);
        }
    }

    public Task Handle(ReviewReceivedEvent e, CancellationToken cancellationToken) =>
        WriteAsync(e.RevieweeUserId, cancellationToken, () => InboxNotification.Create(
            e.RevieweeUserId, InboxNotificationTypes.NewReview, $"New {e.Rating}-star review",
            string.IsNullOrWhiteSpace(e.Comment) ? "A buyer rated their experience with you." : Truncate($"“{e.Comment.Trim()}”"),
            e.ListingId, null, _dateTime.UtcNow));

    private async Task WriteForSellerAsync(Guid sellerProfileId, CancellationToken cancellationToken, Func<Guid, InboxNotification> build)
    {
        var userId = await _dbContext.SellerProfiles
            .Where(s => s.Id == sellerProfileId)
            .Select(s => (Guid?)s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is { } id)
        {
            await WriteAsync(id, cancellationToken, () => build(id));
        }
    }

    private async Task WriteAsync(Guid userId, CancellationToken cancellationToken, Func<InboxNotification> build)
    {
        try
        {
            _dbContext.InboxNotifications.Add(build());
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write an inbox notification for user {UserId}", userId);
        }
    }

    private async Task<string> ListingTitleAsync(Guid listingId, CancellationToken cancellationToken) =>
        await _dbContext.Listings.Where(l => l.Id == listingId).Select(l => l.Title).FirstOrDefaultAsync(cancellationToken)
        ?? "Your ad";

    private static string Truncate(string text) =>
        text.Length <= PreviewLength ? text : text[..(PreviewLength - 1)].TrimEnd() + "…";
}
