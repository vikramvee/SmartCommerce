using MediatR;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Application.Commands;

public sealed class PlaceOrderCommandHandler
    : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IOrderRepository repository,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task<PlaceOrderResult> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Placing order for tenant {TenantId}, customer {CustomerId}",
            command.TenantId, command.CustomerId);

        var items = command.Items.Select(i =>
            OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)
        ).ToList();

        var order = Order.Create(command.TenantId, command.CustomerId, items);

        await _repository.SaveAsync(order, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} placed successfully for tenant {TenantId}",
            order.OrderId, order.TenantId);

        return new PlaceOrderResult(
            order.OrderId,
            order.TenantId,
            order.Total,
            order.Status.ToString()
        );
    }
}