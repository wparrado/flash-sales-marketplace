using System.Diagnostics;

namespace FlashSales.Api.Middleware;

/// <summary>
/// Global traceability: accepts the X-Correlation-ID propagated by the frontend
/// (or mints one), echoes it on the response, stamps the current OpenTelemetry
/// activity, and opens a logging scope so EVERY structured log line emitted
/// while handling the request automatically carries the correlation id.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await next(context);
        }
    }

    private static string Resolve(HttpContext context) =>
        context.Request.Headers.TryGetValue(HeaderName, out var values)
        && values.FirstOrDefault() is { Length: > 0 and <= 128 } incoming
            ? incoming
            : Guid.NewGuid().ToString();
}

public static class CorrelationIdContextExtensions
{
    public static string CorrelationId(this HttpContext context) =>
        context.Items[CorrelationIdMiddleware.ItemKey] as string ?? "unknown";
}
