namespace OrderService.Infrastructure.Tenancy;

public sealed class TenantIsolationException : Exception
{
    public string RequestedTenantId { get; }
    public string ActualTenantId { get; }

    public TenantIsolationException(string requestedTenantId, string actualTenantId)
        : base($"Tenant isolation violation: requested '{requestedTenantId}' " +
               $"but record belongs to '{actualTenantId}'")
    {
        RequestedTenantId = requestedTenantId;
        ActualTenantId    = actualTenantId;
    }
}