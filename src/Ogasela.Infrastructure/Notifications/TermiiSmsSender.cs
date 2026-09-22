using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogasela.Application.Accounts;
using Ogasela.Application.Notifications.Interfaces;

namespace Ogasela.Infrastructure.Notifications;

public sealed class TermiiSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly TermiiSettings _settings;
    private readonly ILogger<TermiiSmsSender> _logger;

    public TermiiSmsSender(HttpClient httpClient, IOptions<TermiiSettings> settings, ILogger<TermiiSmsSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        var payload = new TermiiSendRequest(
            To: NigerianPhoneNumber.ToInternationalFormat(phoneNumber),
            From: _settings.SenderId,
            Sms: message,
            Type: "plain",
            Channel: "generic",
            ApiKey: _settings.ApiKey);

        using var response = await _httpClient.PostAsJsonAsync("api/sms/send", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Termii SMS send failed with status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }
    }

    private sealed record TermiiSendRequest(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("sms")] string Sms,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("api_key")] string ApiKey);
}
