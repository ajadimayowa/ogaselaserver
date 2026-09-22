using Serilog.Context;

namespace Ogasela.Api.Middleware;

/// <summary>
/// Tags every request with a correlation id: reused from an inbound X-Correlation-Id header when
/// the caller (or an upstream gateway) already set one, otherwise freshly generated. Pushed into
/// Serilog's LogContext (picked up by every log line for the request via the Enrich.FromLogContext()
/// already configured in Program.cs) and echoed back on the response so a user's bug report or a
/// support ticket can be matched straight back to the exact log lines for their request.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
