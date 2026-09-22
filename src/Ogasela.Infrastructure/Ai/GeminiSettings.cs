namespace Ogasela.Infrastructure.Ai;

public sealed class GeminiSettings
{
    public const string SectionName = "Ai:Gemini";

    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gemini-2.5-flash-lite";
}
