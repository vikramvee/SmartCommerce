using OrderService.Domain.Common;

namespace OrderService.Domain.Events;

public sealed record OrderCancelledEvent : DomainEvent
{
    public required String OrderId { get; init; }
    public required string TenantId { get; init; }
    public required string Reason { get; init; }
    public override string EventType => "order.cancelled";
}