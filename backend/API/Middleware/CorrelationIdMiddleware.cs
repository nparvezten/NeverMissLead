using Serilog.Context;

namespace NeverMissLead.API.Middleware;

/// <summary>
/// Reads or generates an <c>X-Correlation-Id</c> header and pushes it into
/// Serilog's log context so every log line for this request is tagged with the same ID.
/// The correlation ID is echoed back in the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context);
    }
}
