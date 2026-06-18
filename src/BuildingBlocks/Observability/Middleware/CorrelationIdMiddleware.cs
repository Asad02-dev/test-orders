using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Common;

namespace Observability.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CorrelationContext is scoped — must be resolved from request services, not injected into the middleware constructor.
        var correlationContext = context.RequestServices.GetRequiredService<CorrelationContext>();

        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            && Guid.TryParse(headerValue, out var parsed))
        {
            correlationContext.CorrelationId = parsed;
        }

        correlationContext.TraceId = context.TraceIdentifier;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationContext.CorrelationId.ToString();
            return Task.CompletedTask;
        });

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationContext.CorrelationId))
        {
            await _next(context);
        }
    }
}
