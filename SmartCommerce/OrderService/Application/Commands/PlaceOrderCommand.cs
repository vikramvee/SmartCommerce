using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Commands;

public sealed record PlaceOrderCommand(
    string TenantId,
    string CustomerId,
    List<PlaceOrderItemDto> Items
) : IRequest<PlaceOrderResult>
{
    public Order ToOrder() => Order.Create(
        TenantId,
        CustomerId,
        Items.Select(i => OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
    )   ;
}

public sealed record PlaceOrderItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public sealed record PlaceOrderResult(
    string OrderId,
    string TenantId,
    decimal Total,
    string Status
);