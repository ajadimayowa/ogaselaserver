using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogasela.Application.Ai;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.Ai;

/// <summary>
/// Generates listing copy with Claude via Anthropic's Messages API
/// (https://docs.claude.com/en/api/messages). When <paramref name="imageRef"/> is supplied it's
/// treated as a publicly reachable HTTPS URL and sent as an image content block by URL - a
/// private-bucket key would need a presigned URL resolved by the caller first, since this class
/// has no opinion on where images actually live.
/// </summary>
public sealed class AnthropicListingCopyGenerator : IListingCopyGenerator
{
    private const int MaxTitleLength = 80;

    private const string SystemPrompt = """
        You write concise, honest marketplace listing copy for Ogasela, a Nigerian classifieds
        platform. Given seller-provided keywords and/or a photo, respond with ONLY a JSON object
        of the exact shape {"title": "...", "description": "..."} and nothing else - no markdown,
        no code fences, no commentary. The title must be under 80 characters. The description
        should be 2-4 plain sentences that are appealing but never invent specific claims (brand,
        condition, specifications) that aren't implied by the input.
        """;

    private readonly HttpClient _httpClient;
    private readonly AnthropicSettings _settings;
    private readonly ILogger<AnthropicListingCopyGenerator> _logger;

    public AnthropicListingCopyGenerator(
        HttpClient httpClient, IOptions<AnthropicSettings> settings, ILogger<AnthropicListingCopyGenerator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public async Task<Result<ListingCopySuggestion>> GenerateDescriptionAsync(
        string? keywords, string? imageRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Anthropic is not configured; cannot generate listing copy");
            return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
        }

        var contentBlocks = new List<object>();
        if (!string.IsNullOrWhiteSpace(imageRef))
        {
            contentBlocks.Add(new { type = "image", source = new { type = "url", url = imageRef } });
        }

        var textPrompt = string.IsNullOrWhiteSpace(keywords)
            ? "Write listing copy based on the attached photo."
            : $"Write listing copy for an item described by these keywords: {keywords}";
        contentBlocks.Add(new { type = "text", text = textPrompt });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", _settings.ApiVersion);
        request.Content = JsonContent.Create(new
        {
            model = _settings.Model,
            max_tokens = 500,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = contentBlocks } }
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic request failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var parsed = JsonSerializer.Deserialize<MessagesResponse>(body, JsonOptions);
            var text = parsed?.Content.FirstOrDefault(c => c.Type == "text")?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Anthropic returned no text content: {Body}", body);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var suggestion = JsonSerializer.Deserialize<GeneratedCopy>(text, JsonOptions);
            if (suggestion is null || string.IsNullOrWhiteSpace(suggestion.Title) || string.IsNullOrWhiteSpace(suggestion.Description))
            {
                _logger.LogWarning("Anthropic response was not the expected JSON shape: {Text}", text);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var title = suggestion.Title.Length > MaxTitleLength ? suggestion.Title[..MaxTitleLength] : suggestion.Title;
            return Result.Success(new ListingCopySuggestion(title, suggestion.Description));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate listing copy via Anthropic");
            return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record MessagesResponse([property: JsonPropertyName("content")] List<ContentBlock> Content);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string? Text);

    private sealed record GeneratedCopy(
        [property: JsonPropertyName("title")] string Title, [property: JsonPropertyName("description")] string Description);
}
