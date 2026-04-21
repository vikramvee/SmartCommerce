namespace SmartCommerce.Contracts.Events;

public sealed record OrderPlacedEvent
{
    public Guid EventId { get; init; }
    public string EventType => "order.placed";
    public DateTime OccurredAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
    public List<OrderLineItem> Items { get; init; } = [];
}

public sealed record OrderLineItem
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}