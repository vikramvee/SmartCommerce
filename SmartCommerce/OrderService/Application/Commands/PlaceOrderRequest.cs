namespace OrderService.Application.Commands;

public sealed record PlaceOrderRequest(
    string CustomerId,
    List<PlaceOrderItemRequest> Items
);

public sealed record PlaceOrderItemRequest(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);