namespace OrderService.Domain.Common;

public interface ITenantEvent
{
    string TenantId { get; }
}
