using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string tenantId, string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByStatusAsync(string tenantId, OrderStatus status, CancellationToken ct = default);
    Task SaveAsync(Order order, CancellationToken ct = default);
}