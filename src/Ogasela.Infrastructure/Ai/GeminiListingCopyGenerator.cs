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
/// Generates listing copy with Gemini via Google's Generative Language API
/// (https://ai.google.dev/api/generate-content), defaulting to gemini-2.5-flash-lite - a small,
/// low-latency/low-cost model well suited to this short, low-stakes generation task.
/// Unlike Anthropic's Messages API, Gemini's inlineData image part needs raw bytes rather than
/// a URL, so when <paramref name="imageRef"/> is supplied (still treated as a publicly
/// reachable HTTPS URL, same contract as the Anthropic implementation) this downloads it first
/// and sends it as base64.
/// </summary>
public sealed class GeminiListingCopyGenerator : IListingCopyGenerator
{
    private const int MaxTitleLength = 80;
    private const string DefaultImageMimeType = "image/jpeg";

    private const string SystemPrompt = """
        You write concise, honest marketplace listing copy for Ogasela, a Nigerian classifieds
        platform. Given seller-provided keywords and/or a photo, respond with ONLY a JSON object
        of the exact shape {"title": "...", "description": "..."} and nothing else - no markdown,
        no code fences, no commentary. The title must be under 80 characters. The description
        should be 2-4 plain sentences that are appealing but never invent specific claims (brand,
        condition, specifications) that aren't implied by the input.
        """;

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiListingCopyGenerator> _logger;

    public GeminiListingCopyGenerator(
        HttpClient httpClient, IOptions<GeminiSettings> settings, ILogger<GeminiListingCopyGenerator> logger)
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
            _logger.LogWarning("Gemini is not configured; cannot generate listing copy");
            return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
        }

        try
        {
            var parts = new List<object>();

            var textPrompt = string.IsNullOrWhiteSpace(keywords)
                ? "Write listing copy based on the attached photo."
                : $"Write listing copy for an item described by these keywords: {keywords}";
            parts.Add(new { text = textPrompt });

            if (!string.IsNullOrWhiteSpace(imageRef))
            {
                var (base64Data, mimeType) = await DownloadAndEncodeImageAsync(imageRef, cancellationToken);
                parts.Add(new { inlineData = new { mimeType, data = base64Data } });
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"/v1beta/models/{_settings.Model}:generateContent");
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);
            request.Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[] { new { role = "user", parts } },
                generationConfig = new { maxOutputTokens = 500, responseMimeType = "application/json" }
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini request failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var parsed = JsonSerializer.Deserialize<GenerateContentResponse>(body, JsonOptions);
            var text = parsed?.Candidates.FirstOrDefault()?.Content?.Parts.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini returned no text content: {Body}", body);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var suggestion = JsonSerializer.Deserialize<GeneratedCopy>(text, JsonOptions);
            if (suggestion is null || string.IsNullOrWhiteSpace(suggestion.Title) || string.IsNullOrWhiteSpace(suggestion.Description))
            {
                _logger.LogWarning("Gemini response was not the expected JSON shape: {Text}", text);
                return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
            }

            var title = suggestion.Title.Length > MaxTitleLength ? suggestion.Title[..MaxTitleLength] : suggestion.Title;
            return Result.Success(new ListingCopySuggestion(title, suggestion.Description));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate listing copy via Gemini");
            return Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed);
        }
    }

    private async Task<(string Base64Data, string MimeType)> DownloadAndEncodeImageAsync(
        string imageUrl, CancellationToken cancellationToken)
    {
        using var imageResponse = await _httpClient.GetAsync(imageUrl, cancellationToken);
        imageResponse.EnsureSuccessStatusCode();

        var mimeType = imageResponse.Content.Headers.ContentType?.MediaType ?? DefaultImageMimeType;
        var bytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return (Convert.ToBase64String(bytes), mimeType);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record GenerateContentResponse([property: JsonPropertyName("candidates")] List<Candidate> Candidates);

    private sealed record Candidate([property: JsonPropertyName("content")] ContentPart? Content);

    private sealed record ContentPart([property: JsonPropertyName("parts")] List<TextPart> Parts);

    private sealed record TextPart([property: JsonPropertyName("text")] string? Text);

    private sealed record GeneratedCopy(
        [property: JsonPropertyName("title")] string Title, [property: JsonPropertyName("description")] string Description);
}
