using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request already has a correlation ID, or generate one
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeader] = correlationId;
        }

        // Add the correlation ID to the response headers so it propagates back to the client
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add the correlation ID to the request scope for logging
        context.Items[CorrelationIdHeader] = correlationId;

        await _next(context);
    }
}
