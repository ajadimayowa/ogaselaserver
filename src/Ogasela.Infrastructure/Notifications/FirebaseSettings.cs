namespace Ogasela.Infrastructure.Notifications;

public sealed class FirebaseSettings
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; init; } = string.Empty;

    /// <summary>The raw JSON contents of a Firebase service account key (not a file path) - set via env var, never committed.</summary>
    public string ServiceAccountJson { get; init; } = string.Empty;
}
