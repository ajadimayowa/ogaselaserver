namespace Ogasela.Application.Accounts.Interfaces;

public interface IOtpService
{
    /// <summary>
    /// Generates a new 6-digit code for the phone number, stores it with a 5-minute TTL, and
    /// returns it so the caller can deliver it (SMS). The code is never logged.
    /// </summary>
    Task<string> GenerateAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies a submitted code against the stored one, enforcing a rolling rate limit of
    /// 5 verify attempts per 15 minutes per phone number.
    /// </summary>
    Task<OtpVerificationResult> VerifyAsync(string phoneNumber, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the currently stored code without consuming it, for resend flows and for
    /// integration tests that stand in for the SMS gateway.
    /// </summary>
    Task<string?> PeekAsync(string phoneNumber, CancellationToken cancellationToken);
}
