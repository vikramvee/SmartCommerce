namespace OrderService.Infrastructure.Tenancy;

public interface ITenantContext
{
    string TenantId { get; }
    bool IsResolved { get; }
}