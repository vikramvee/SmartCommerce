using OrderService.Domain.Common;

namespace SmartCommerce.Domain.Orders.Events;

public sealed record OrderPlacedEvent : DomainEvent
{
    public String OrderId { get; init; } = string.Empty;
    public string TenantId { get; init; } = default!;
    public string CustomerId { get; init; } = default!;
    public List<OrderLineItem> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; } = DateTime.UtcNow;
    public override string EventType => throw new NotImplementedException();
}

public record OrderLineItem
{
    public string ProductId { get; init; } = default!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}