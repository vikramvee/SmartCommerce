namespace OrderService.Infrastructure.Correlation;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context, CorrelationIdAccessor accessor)
    {
        // Use incoming header or generate a new one
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        accessor.Set(correlationId);

        // Echo it back on the response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Push into Serilog's LogContext for this request
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}