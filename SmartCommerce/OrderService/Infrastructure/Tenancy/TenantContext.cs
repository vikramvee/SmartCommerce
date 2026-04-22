namespace OrderService.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private string? _tenantId;

    public string TenantId => _tenantId
        ?? throw new InvalidOperationException(
            "TenantId has not been resolved. Ensure TenantMiddleware is registered.");

    public bool IsResolved => _tenantId is not null;

    internal void Set(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _tenantId = tenantId;
    }
}