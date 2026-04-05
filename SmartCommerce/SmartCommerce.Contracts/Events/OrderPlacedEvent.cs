namespace SmartCommerce.Contracts.Events;

public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public string TenantId { get; init; } = default!;
    public string CustomerId { get; init; } = default!;
    public List<OrderLineItem> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; } = DateTime.UtcNow;
}

public record OrderLineItem
{
    public string ProductId { get; init; } = default!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}