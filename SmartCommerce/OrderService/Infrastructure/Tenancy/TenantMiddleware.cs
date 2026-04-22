namespace OrderService.Infrastructure.Tenancy;

public sealed class TenantMiddleware
{
    private const string HeaderName = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing required header: X-Tenant-Id"
            });
            return;
        }

        tenantContext.Set(tenantId);

        // Echo back + push into Serilog scope
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = tenantId;
            return Task.CompletedTask;
        });

        using (Serilog.Context.LogContext.PushProperty("TenantId", tenantId))
        {
            await _next(context);
        }
    }
}