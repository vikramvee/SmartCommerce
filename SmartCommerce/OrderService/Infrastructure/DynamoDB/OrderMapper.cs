using OrderService.Domain.Entities;
using SmartCommerce.Domain.Entities;

namespace OrderService.Infrastructure.DynamoDB;

public static class OrderMapper
{
    public static OrderDynamoDbModel ToModel(Order order)
    {
        return new OrderDynamoDbModel
        {
            PK       = $"TENANT#{order.TenantId}",
            SK       = $"ORDER#{order.OrderId}",
            GSI1PK   = $"TENANT#{order.TenantId}#STATUS#{order.Status}",
            GSI1SK   = $"ORDER#{order.OrderId}",
            OrderId    = order.OrderId,
            TenantId   = order.TenantId,
            CustomerId = order.CustomerId,
            Status     = order.Status,
            TotalAmount      = order.Total,
            CreatedAt  = order.CreatedAt,
            UpdatedAt  = order.UpdatedAt??DateTime.Now,
            Items      = order.Items.Select(i => new OrderItemDynamoDbModel
            {
                ItemId      = i.ItemId.ToString(),
                ProductId   = i.ProductId,
                ProductName = i.ProductName,
                Quantity    = i.Quantity,
                UnitPrice   = i.UnitPrice
            }).ToList()
        };
    }

    public static Order ToDomain(OrderDynamoDbModel model)
    {
        var items = model.Items.Select(i => OrderItem.Reconstitute(
            Guid.Parse(i.ItemId),
            i.ProductId,
            i.ProductName,
            i.Quantity,
            i.UnitPrice
        ));

        var status    = Enum.Parse<OrderStatus>(model.Status.ToString(), ignoreCase: true);
        var createdAt = DateTime.Parse(model.CreatedAt.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime? updatedAt = DateTime.Parse(model.UpdatedAt.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
            ;

        return Order.Reconstitute(
            model.OrderId,
            model.TenantId,
            model.CustomerId,
            status,
            createdAt,
            updatedAt,
            items
        );
    }
}