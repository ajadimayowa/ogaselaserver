namespace Ogasela.Infrastructure.Notifications;

public sealed class TermiiSettings
{
    public const string SectionName = "Termii";

    public string BaseUrl { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SenderId { get; init; } = string.Empty;
}
