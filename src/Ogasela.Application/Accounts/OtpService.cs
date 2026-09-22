using System.Security.Cryptography;
using Ogasela.Application.Accounts.Interfaces;

namespace Ogasela.Application.Accounts;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan CodeTimeToLive = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private const int MaxVerifyAttempts = 5;

    private readonly IOtpStore _store;

    public OtpService(IOtpStore store)
    {
        _store = store;
    }

    public async Task<string> GenerateAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await _store.SetCodeAsync(phoneNumber, code, CodeTimeToLive, cancellationToken);
        return code;
    }

    public async Task<OtpVerificationResult> VerifyAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        var attempts = await _store.IncrementAttemptsAsync(phoneNumber, RateLimitWindow, cancellationToken);
        if (attempts > MaxVerifyAttempts)
        {
            return OtpVerificationResult.RateLimited;
        }

        var storedCode = await _store.GetCodeAsync(phoneNumber, cancellationToken);
        if (storedCode is null)
        {
            return OtpVerificationResult.Expired;
        }

        if (!string.Equals(storedCode, code, StringComparison.Ordinal))
        {
            return OtpVerificationResult.InvalidCode;
        }

        await _store.DeleteCodeAsync(phoneNumber, cancellationToken);
        await _store.ResetAttemptsAsync(phoneNumber, cancellationToken);
        return OtpVerificationResult.Success;
    }

    public Task<string?> PeekAsync(string phoneNumber, CancellationToken cancellationToken) =>
        _store.GetCodeAsync(phoneNumber, cancellationToken);
}
