using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Ogasela.Api.RateLimiting;

/// <summary>
/// Endpoint-level rate limiting, layered on top of (not a replacement for) the business-rule
/// limits already enforced elsewhere - most notably OtpService's own 5-verify-attempts-per-15-
/// minutes-per-phone-number limit, which still applies underneath the Auth policy below. These
/// two policies exist specifically for /api/v1/auth/* (credential-stuffing / OTP-SMS-pumping is
/// the classic abuse vector there) and the message-send endpoint (spam flooding a conversation).
///
/// Permit counts are read from RateLimitSettings (configuration), not hardcoded, specifically so
/// appsettings.Development.json can set them high enough that local dev and the integration test
/// suite - which registers/logs in dozens of times a second from the same loopback address across
/// many test classes - never trips them, while appsettings.json keeps tight production defaults.
/// Those production defaults are deliberately conservative, clearly-labeled numbers rather than
/// figures pulled from Phase 1/7's original requirements text (not available verbatim in this
/// session) - tune RateLimitSettings if the product has stricter published limits.
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string MessagingPolicy = "messaging";
    public const string MarketingPolicy = "marketing";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var settings = GetSettings(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = settings.AuthPermitsPerMinute,
                        QueueLimit = 0
                    });
            });

            // Partitioned per authenticated user (not IP) so one busy office/NAT IP never
            // throttles every seller/buyer behind it.
            options.AddPolicy(MessagingPolicy, httpContext =>
            {
                var settings = GetSettings(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? GetClientIpAddress(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = settings.MessagingPermitsPerMinute,
                        QueueLimit = 0
                    });
            });

            // Anonymous marketing-site forms (contact, tester signup, deletion requests) - IP
            // partitioned like Auth, since there's no authenticated user yet.
            options.AddPolicy(MarketingPolicy, httpContext =>
            {
                var settings = GetSettings(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = settings.MarketingPermitsPerMinute,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    private static RateLimitSettings GetSettings(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    private static string GetClientIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitsPerMinute { get; init; } = 10;

    public int MessagingPermitsPerMinute { get; init; } = 20;

    public int MarketingPermitsPerMinute { get; init; } = 10;
}
