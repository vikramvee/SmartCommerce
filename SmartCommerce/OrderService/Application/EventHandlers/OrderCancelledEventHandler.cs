using OrderService.Application.Interfaces;
using OrderService.Domain.Orders.Events;

namespace OrderService.Application.EventHandlers;

public sealed class OrderCancelledEventHandler : IEventHandler<OrderCancelledEvent>
{
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(ILogger<OrderCancelledEventHandler> logger)
        => _logger = logger;

    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EventHandler] OrderCancelled received. OrderId: {OrderId}, TenantId: {TenantId}, Reason: {Reason}",
            domainEvent.OrderId,
            domainEvent.TenantId,
            domainEvent.Reason);

        // Week 5: notify customer of cancellation
        // Week 5: release inventory reservation

        return Task.CompletedTask;
    }
}