namespace Ogasela.Application.Marketing.Interfaces;

public interface ITurnstileVerifier
{
    /// <summary>Verifies a Cloudflare Turnstile token against Cloudflare's siteverify endpoint. Returns false for a missing, invalid, or already-used token, or if Cloudflare's service itself is unreachable.</summary>
    Task<bool> VerifyAsync(string token, CancellationToken cancellationToken);
}
