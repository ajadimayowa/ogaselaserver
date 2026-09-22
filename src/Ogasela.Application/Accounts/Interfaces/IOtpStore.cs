namespace Ogasela.Application.Accounts.Interfaces;

/// <summary>
/// Low-level, provider-agnostic storage for OTP codes and verify-attempt counters.
/// Implemented against Redis in <c>Ogasela.Infrastructure</c>; the rate-limiting and
/// verification rules themselves live in <see cref="IOtpService"/> so they can be unit
/// tested without a real Redis instance.
/// </summary>
public interface IOtpStore
{
    Task SetCodeAsync(string phoneNumber, string code, TimeSpan timeToLive, CancellationToken cancellationToken);

    Task<string?> GetCodeAsync(string phoneNumber, CancellationToken cancellationToken);

    Task DeleteCodeAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments the verify-attempt counter for the phone number, starting a
    /// new rolling window if none is active, and returns the counter's new value.
    /// </summary>
    Task<int> IncrementAttemptsAsync(string phoneNumber, TimeSpan window, CancellationToken cancellationToken);

    Task ResetAttemptsAsync(string phoneNumber, CancellationToken cancellationToken);
}
