using SmartCommerce.Contracts.Events;

namespace NotificationService.Handlers;

public sealed class OrderNotificationHandler
{
    private readonly ILogger<OrderNotificationHandler> _logger;

    public OrderNotificationHandler(ILogger<OrderNotificationHandler> logger)
        => _logger = logger;

    public Task HandleOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken ct)
    {
        // Week 7: replace with real email via SES
        _logger.LogInformation(
            "[Notification] Order confirmation email sent. " +
            "OrderId: {OrderId}, CustomerId: {CustomerId}, " +
            "TenantId: {TenantId}, Total: {Total}",
            orderPlaced.OrderId,
            orderPlaced.CustomerId,
            orderPlaced.TenantId,
            orderPlaced.TotalAmount);

        return Task.CompletedTask;
    }
}