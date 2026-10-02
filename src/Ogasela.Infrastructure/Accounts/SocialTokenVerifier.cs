using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.Accounts;

/// <summary>
/// Google: the ID token is validated by Google.Apis.Auth (signature, issuer, expiry, audience).
/// Apple: the identity token is validated against Apple's published signing keys, fetched and
/// cached through its OpenID discovery document.
/// Facebook: there's no ID token for this flow, so the access token is checked with Graph's
/// debug_token (it must be valid and issued to our app) before reading the profile with it.
/// </summary>
public sealed class SocialTokenVerifier : ISocialTokenVerifier
{
    private const string AppleIssuer = "https://appleid.apple.com";

    // One per process: it caches Apple's signing keys and refreshes them on its own schedule.
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> AppleConfiguration = new(
        $"{AppleIssuer}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever { RequireHttps = true });

    private readonly HttpClient _httpClient;
    private readonly SocialAuthSettings _settings;
    private readonly ILogger<SocialTokenVerifier> _logger;

    public SocialTokenVerifier(HttpClient httpClient, IOptions<SocialAuthSettings> settings, ILogger<SocialTokenVerifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<Result<SocialIdentity>> VerifyAsync(ExternalLoginProvider provider, string token, CancellationToken cancellationToken) =>
        provider switch
        {
            ExternalLoginProvider.Google => VerifyGoogleAsync(token),
            ExternalLoginProvider.Apple => VerifyAppleAsync(token, cancellationToken),
            ExternalLoginProvider.Facebook => VerifyFacebookAsync(token, cancellationToken),
            _ => Task.FromResult(Result.Failure<SocialIdentity>(AccountErrors.SocialProviderNotConfigured))
        };

    private async Task<Result<SocialIdentity>> VerifyGoogleAsync(string idToken)
    {
        var clientIds = _settings.Google.ClientIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
        if (clientIds.Length == 0)
        {
            return Result.Failure<SocialIdentity>(AccountErrors.SocialProviderNotConfigured);
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = clientIds });

            return Result.Success(new SocialIdentity(payload.Subject, payload.Email, payload.EmailVerified, payload.Name));
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Rejected a Google ID token");
            return Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid);
        }
    }

    private async Task<Result<SocialIdentity>> VerifyAppleAsync(string identityToken, CancellationToken cancellationToken)
    {
        var audiences = _settings.Apple.Audiences.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        if (audiences.Length == 0)
        {
            return Result.Failure<SocialIdentity>(AccountErrors.SocialProviderNotConfigured);
        }

        var configuration = await AppleConfiguration.GetConfigurationAsync(cancellationToken);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(identityToken, new TokenValidationParameters
        {
            ValidIssuer = AppleIssuer,
            ValidAudiences = audiences,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true
        });

        if (!result.IsValid || result.SecurityToken is not Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt)
        {
            _logger.LogWarning(result.Exception, "Rejected an Apple identity token");
            return Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid);
        }

        jwt.TryGetPayloadValue<string>("email", out var email);
        // Apple sends email_verified as either a boolean or the string "true".
        var emailVerified =
            (jwt.TryGetPayloadValue<bool>("email_verified", out var verifiedBool) && verifiedBool) ||
            (jwt.TryGetPayloadValue<string>("email_verified", out var verifiedText) && verifiedText == "true");

        // Apple never puts the user's name in the token; the app passes it on first sign-in.
        return Result.Success(new SocialIdentity(jwt.Subject, email, emailVerified, Name: null));
    }

    private async Task<Result<SocialIdentity>> VerifyFacebookAsync(string accessToken, CancellationToken cancellationToken)
    {
        var appId = _settings.Facebook.AppId;
        var appSecret = _settings.Facebook.AppSecret;
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            return Result.Failure<SocialIdentity>(AccountErrors.SocialProviderNotConfigured);
        }

        try
        {
            var debugUrl = "https://graph.facebook.com/debug_token" +
                $"?input_token={Uri.EscapeDataString(accessToken)}" +
                $"&access_token={Uri.EscapeDataString($"{appId}|{appSecret}")}";
            var debug = await _httpClient.GetFromJsonAsync<FacebookDebugTokenResponse>(debugUrl, cancellationToken);

            if (debug?.Data is not { IsValid: true } data || data.AppId != appId || string.IsNullOrEmpty(data.UserId))
            {
                return Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid);
            }

            var profileUrl = "https://graph.facebook.com/me?fields=id,name,email" +
                $"&access_token={Uri.EscapeDataString(accessToken)}";
            var profile = await _httpClient.GetFromJsonAsync<FacebookProfile>(profileUrl, cancellationToken);

            if (profile is null || profile.Id != data.UserId)
            {
                return Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid);
            }

            // Facebook only returns an email the user has confirmed with Facebook.
            return Result.Success(new SocialIdentity(profile.Id, profile.Email, EmailVerified: profile.Email is not null, profile.Name));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Facebook token check failed");
            return Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid);
        }
    }

    private sealed record FacebookDebugTokenResponse([property: JsonPropertyName("data")] FacebookDebugTokenData? Data);

    private sealed record FacebookDebugTokenData(
        [property: JsonPropertyName("is_valid")] bool IsValid,
        [property: JsonPropertyName("app_id")] string? AppId,
        [property: JsonPropertyName("user_id")] string? UserId);

    private sealed record FacebookProfile(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("email")] string? Email);
}
