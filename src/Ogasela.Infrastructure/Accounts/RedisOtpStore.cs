using Ogasela.Application.Accounts.Interfaces;
using StackExchange.Redis;

namespace Ogasela.Infrastructure.Accounts;

public sealed class RedisOtpStore : IOtpStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisOtpStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string CodeKey(string phoneNumber) => $"otp:code:{phoneNumber}";

    private static string AttemptsKey(string phoneNumber) => $"otp:attempts:{phoneNumber}";

    public Task SetCodeAsync(string phoneNumber, string code, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        return db.StringSetAsync(CodeKey(phoneNumber), code, timeToLive);
    }

    public async Task<string?> GetCodeAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CodeKey(phoneNumber));
        return value.HasValue ? value.ToString() : null;
    }

    public Task DeleteCodeAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        return db.KeyDeleteAsync(CodeKey(phoneNumber));
    }

    public async Task<int> IncrementAttemptsAsync(string phoneNumber, TimeSpan window, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var key = AttemptsKey(phoneNumber);

        var attempts = await db.StringIncrementAsync(key);
        if (attempts == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return (int)attempts;
    }

    public Task ResetAttemptsAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        return db.KeyDeleteAsync(AttemptsKey(phoneNumber));
    }
}
