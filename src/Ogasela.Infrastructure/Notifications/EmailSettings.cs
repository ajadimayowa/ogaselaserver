namespace Ogasela.Infrastructure.Notifications;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpServer { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string Login { get; init; } = string.Empty;

    public string SmtpKey { get; init; } = string.Empty;

    public string SenderEmail { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;
}
