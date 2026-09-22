namespace Ogasela.Infrastructure.Ai;

public sealed class AnthropicSettings
{
    public const string SectionName = "Ai:Anthropic";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-sonnet-5";

    public string ApiVersion { get; init; } = "2023-06-01";
}
