using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ogasela.Application.Accounts.Interfaces;

namespace Ogasela.Application.Accounts;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan CodeTimeToLive = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private const int MaxVerifyAttempts = 5;

    private readonly IOtpStore _store;
    private readonly string? _masterCode;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IOtpStore store, IOptions<OtpSettings>? settings = null, ILogger<OtpService>? logger = null)
    {
        _store = store;
        _masterCode = string.IsNullOrWhiteSpace(settings?.Value.MasterCode) ? null : settings.Value.MasterCode.Trim();
        _logger = logger ?? NullLogger<OtpService>.Instance;
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

        // The master code (OtpSettings.MasterCode) passes any check, whether or not a code was sent.
        // Still behind the attempt limit above, so it can't be brute-forced faster than a real code.
        if (IsMasterCode(code))
        {
            _logger.LogWarning("OTP check for {OtpKey} passed with the master code (Default_Global_OTP)", phoneNumber);
            await _store.DeleteCodeAsync(phoneNumber, cancellationToken);
            await _store.ResetAttemptsAsync(phoneNumber, cancellationToken);
            return OtpVerificationResult.Success;
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

    private bool IsMasterCode(string code) =>
        _masterCode is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code.Trim()), Encoding.UTF8.GetBytes(_masterCode));

    public Task<string?> PeekAsync(string phoneNumber, CancellationToken cancellationToken) =>
        _store.GetCodeAsync(phoneNumber, cancellationToken);
}
