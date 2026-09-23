using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogasela.Application.Marketing.Interfaces;

namespace Ogasela.Infrastructure.Marketing;

public sealed class CloudflareTurnstileVerifier : ITurnstileVerifier
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly ILogger<CloudflareTurnstileVerifier> _logger;

    public CloudflareTurnstileVerifier(
        HttpClient httpClient, IOptions<TurnstileSettings> settings, ILogger<CloudflareTurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var payload = new SiteverifyRequest(_settings.SecretKey, token);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("siteverify", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Turnstile siteverify failed with status {StatusCode}: {ResponseBody}", (int)response.StatusCode, body);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>(cancellationToken);
            if (result is { Success: true })
            {
                return true;
            }

            _logger.LogWarning(
                "Turnstile token rejected: {ErrorCodes}", string.Join(", ", result?.ErrorCodes ?? []));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach Cloudflare Turnstile siteverify");
            return false;
        }
    }

    private sealed record SiteverifyRequest(
        [property: JsonPropertyName("secret")] string Secret,
        [property: JsonPropertyName("response")] string Response);

    private sealed record SiteverifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
}
