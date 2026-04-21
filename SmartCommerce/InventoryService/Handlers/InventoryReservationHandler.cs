using SmartCommerce.Contracts.Events;

namespace InventoryService.Handlers;

public sealed class InventoryReservationHandler
{
    private readonly ILogger<InventoryReservationHandler> _logger;

    public InventoryReservationHandler(ILogger<InventoryReservationHandler> logger)
        => _logger = logger;

    public Task HandleOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken ct)
    {
        // Week 8: replace with real DynamoDB inventory check and reservation
        foreach (var item in orderPlaced.Items)
        {
            _logger.LogInformation(
                "[Inventory] Reserving stock. " +
                "OrderId: {OrderId}, ProductId: {ProductId}, " +
                "Quantity: {Quantity}, TenantId: {TenantId}",
                orderPlaced.OrderId,
                item.ProductId,
                item.Quantity,
                orderPlaced.TenantId);
        }

        _logger.LogInformation(
            "[Inventory] Reservation complete. OrderId: {OrderId}, Items: {ItemCount}",
            orderPlaced.OrderId,
            orderPlaced.Items.Count);

        return Task.CompletedTask;
    }
}