using Ogasela.Application.Accounts.Interfaces;

namespace Ogasela.UnitTests.TestSupport;

/// <summary>
/// In-memory stand-in for <see cref="IOtpStore"/> used to unit test <c>OtpService</c>'s
/// rate-limiting and verification rules without a real Redis instance. TTL/window
/// expiry is honoured using <see cref="DateTime.UtcNow"/>.
/// </summary>
public sealed class FakeOtpStore : IOtpStore
{
    private readonly Dictionary<string, (string Code, DateTime ExpiresAt)> _codes = new();
    private readonly Dictionary<string, (int Count, DateTime WindowExpiresAt)> _attempts = new();

    public Task SetCodeAsync(string phoneNumber, string code, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        _codes[phoneNumber] = (code, DateTime.UtcNow.Add(timeToLive));
        return Task.CompletedTask;
    }

    public Task<string?> GetCodeAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        if (_codes.TryGetValue(phoneNumber, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult<string?>(entry.Code);
        }

        return Task.FromResult<string?>(null);
    }

    public Task DeleteCodeAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        _codes.Remove(phoneNumber);
        return Task.CompletedTask;
    }

    public Task<int> IncrementAttemptsAsync(string phoneNumber, TimeSpan window, CancellationToken cancellationToken)
    {
        if (_attempts.TryGetValue(phoneNumber, out var entry) && entry.WindowExpiresAt > DateTime.UtcNow)
        {
            entry.Count++;
            _attempts[phoneNumber] = entry;
            return Task.FromResult(entry.Count);
        }

        _attempts[phoneNumber] = (1, DateTime.UtcNow.Add(window));
        return Task.FromResult(1);
    }

    public Task ResetAttemptsAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        _attempts.Remove(phoneNumber);
        return Task.CompletedTask;
    }
}
