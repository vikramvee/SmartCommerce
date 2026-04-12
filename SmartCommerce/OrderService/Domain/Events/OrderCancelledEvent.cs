using OrderService.Domain.Common;

namespace OrderService.Domain.Orders.Events;

public sealed record OrderCancelledEvent : DomainEvent,ITenantEvent
{
    public required String OrderId { get; init; }
    public required string TenantId { get; init; }
    public required string Reason { get; init; }
    public override string EventType => "order.cancelled";
}