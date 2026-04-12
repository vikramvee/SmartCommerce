using MediatR;

namespace OrderService.Application.Commands;

public sealed record PlaceOrderCommand(
    string TenantId,
    string CustomerId,
    List<PlaceOrderItemDto> Items
) : IRequest<PlaceOrderResult>;

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