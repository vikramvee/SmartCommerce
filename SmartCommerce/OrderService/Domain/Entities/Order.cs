using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.Orders.Events;


namespace OrderService.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public string OrderId { get; private set; } = default!;
    public string TenantId { get; private set; } = default!;
    public string CustomerId { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.LineTotal);
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Order() { } // DynamoDB deserialization


    public static Order Create(string tenantId, string customerId, IEnumerable<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(items));

        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(itemList);

        order.RaiseDomainEvent(new OrderPlacedEvent
        {
            OrderId = order.OrderId,
            TenantId = order.TenantId,
            TotalAmount = order.Total
        });

        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm an order in '{Status}' status.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOperationException($"Cannot cancel an order in '{Status}' status.");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderCancelledEvent
        {
            OrderId = OrderId,
            TenantId = TenantId,
            Reason = reason
        });
    }

    internal static Order Reconstitute(
    string orderId, string tenantId, string customerId,
    OrderStatus status, DateTime createdAt, DateTime? updatedAt,
    IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            OrderId    = orderId,
            TenantId   = tenantId,
            CustomerId = customerId,
            Status     = status,
            CreatedAt  = createdAt,
            UpdatedAt  = updatedAt
        };
        order._items.AddRange(items);
        return order;
    }
}