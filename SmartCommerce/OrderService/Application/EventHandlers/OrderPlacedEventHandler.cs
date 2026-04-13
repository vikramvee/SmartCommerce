using OrderService.Application.Interfaces;
using OrderService.Domain.Orders.Events;

namespace OrderService.Application.EventHandlers;

public sealed class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(OrderPlacedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EventHandler] OrderPlaced received. OrderId: {OrderId}, TenantId: {TenantId}, Total: {Total}",
            domainEvent.OrderId,
            domainEvent.TenantId,
            domainEvent.TotalAmount);

        // Week 5: call InventoryService to reserve stock
        // Week 5: call NotificationService to email customer

        return Task.CompletedTask;
    }
}