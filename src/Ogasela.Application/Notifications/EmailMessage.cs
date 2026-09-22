namespace Ogasela.Application.Notifications;

public sealed record EmailMessage(string ToEmail, string? ToName, string Subject, string HtmlBody);
