using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Interfaces;

/// <summary>
/// Checks a token the app got from a social sign-in provider and returns who it belongs to.
/// Google and Apple send a signed ID token (verified against the provider's published keys and
/// our client ids); Facebook sends an access token (checked with the Graph API against our app).
/// Fails with SocialLogin.ProviderNotConfigured when that provider's credentials aren't set.
/// </summary>
public interface ISocialTokenVerifier
{
    Task<Result<SocialIdentity>> VerifyAsync(ExternalLoginProvider provider, string token, CancellationToken cancellationToken);
}

/// <summary>EmailVerified is the provider's own claim; only a verified email is used to match an existing account.</summary>
public sealed record SocialIdentity(string ProviderUserId, string? Email, bool EmailVerified, string? Name);
