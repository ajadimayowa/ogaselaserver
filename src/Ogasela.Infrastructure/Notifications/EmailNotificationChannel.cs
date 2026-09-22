using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Notifications;

namespace Ogasela.Infrastructure.Notifications;

/// <summary>Delivers notifications over email by reusing Phase 1's IEmailSender (Brevo/SMTP via MailKit) and branded template.</summary>
public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(
        IApplicationDbContext dbContext, IEmailSender emailSender, ILogger<EmailNotificationChannel> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<bool> SendAsync(Guid userId, string type, string payload, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user?.Email))
        {
            _logger.LogInformation("Skipping email notification {Type} for user {UserId} - no email on file", type, userId);
            return false;
        }

        var (heading, body, eyebrow) = BuildContent(type);
        var html = BrandedEmailTemplate.Render(previewText: heading, heading: heading, bodyParagraphs: [body], eyebrow: eyebrow);

        try
        {
            await _emailSender.SendAsync(new EmailMessage(user.Email, null, heading, html), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email notification {Type} to user {UserId}", type, userId);
            return false;
        }
    }

    private static (string Heading, string Body, string Eyebrow) BuildContent(string type) => type switch
    {
        NotificationTypes.ListingExpiringSoon => (
            "Your listing is expiring soon",
            "One of your listings will expire within 24 hours. Renew it to keep it visible to buyers.",
            "Listing update"),
        NotificationTypes.ListingExpired => (
            "Your listing has expired",
            "One of your listings has expired and is no longer visible to buyers. Repost it to relist.",
            "Listing update"),
        NotificationTypes.VerificationDecision => (
            "Your verification status has been updated",
            "Your seller verification has a new decision. Check your account for details.",
            "Verification"),
        _ => (type, "You have a new notification on Ogasela.", "Notification")
    };
}
