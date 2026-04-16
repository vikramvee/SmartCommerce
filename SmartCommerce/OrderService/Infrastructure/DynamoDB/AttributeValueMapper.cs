using Amazon.DynamoDBv2.Model;
using OrderService.Infrastructure.Outbox;

namespace OrderService.Infrastructure.DynamoDB;

public static class AttributeValueMapper
{
    public static Dictionary<string, AttributeValue> ToAttributeMap(OrderDynamoDbModel m)
    {
        var map = new Dictionary<string, AttributeValue>
        {
            ["PK"]         = new() { S = m.PK },
            ["SK"]         = new() { S = m.SK },
            ["GSI1PK"]     = new() { S = m.GSI1PK },
            ["GSI1SK"]     = new() { S = m.GSI1SK },
            ["OrderId"]    = new() { S = m.OrderId },
            ["TenantId"]   = new() { S = m.TenantId },
            ["CustomerId"] = new() { S = m.CustomerId },
            ["Status"]     = new() { S = m.Status.ToString() },
            ["Total"]      = new() { N = m.TotalAmount.ToString("F2") },
            ["EntityType"] = new() { S = m.EntityType },
            ["CreatedAt"]  = new() { S = m.CreatedAt.ToString() },
            ["Items"]      = new() { L = m.Items.Select(ToItemAttributeValue).ToList() }
        };

        
            map["UpdatedAt"] = new() { S = m.UpdatedAt.ToString() };

        return map;
    }

    public static Dictionary<string, AttributeValue> ToOutboxAttributeMap(OutboxDynamoDbModel m)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["PK"]            = new() { S = m.PK },
            ["SK"]            = new() { S = m.SK },
            ["GSI1PK"]        = new() { S = m.GSI1PK },
            ["GSI1SK"]        = new() { S = m.GSI1SK },
            ["MessageId"]     = new() { S = m.MessageId },
            ["TenantId"]      = new() { S = m.TenantId },
            ["EventType"]     = new() { S = m.EventType },
            ["Payload"]       = new() { S = m.Payload },
            ["CreatedAt"]     = new() { S = m.CreatedAt },
            ["EntityType"]    = new() { S = m.EntityType },
            ["CorrelationId"] = new() { S = m.CorrelationId }  // ← add
        };
    }

    public static OrderDynamoDbModel ToOrderModel(Dictionary<string, AttributeValue> item)
    {
        return new OrderDynamoDbModel
        {
            PK         = item["PK"].S,
            SK         = item["SK"].S,
            GSI1PK     = item["GSI1PK"].S,
            GSI1SK     = item["GSI1SK"].S,
            OrderId    = item["OrderId"].S,
            TenantId   = item["TenantId"].S,
            CustomerId = item["CustomerId"].S,
            Status     = (OrderStatus)(Enum.TryParse(typeof(OrderStatus), item["Status"].S, out var result) ? result : null),
            TotalAmount      = decimal.Parse(item["Total"].N),
            CreatedAt  = DateTime.Parse(item["CreatedAt"].S),
            UpdatedAt  = DateTime.Parse(item.TryGetValue("UpdatedAt", out var ua) ? ua.S : null),
            Items      = item.TryGetValue("Items", out var items)
                ? items.L.Select(ToOrderItemModel).ToList()
                : []
        };
    }

    private static AttributeValue ToItemAttributeValue(OrderItemDynamoDbModel i) =>
        new()
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["ItemId"]      = new() { S = i.ItemId },
                ["ProductId"]   = new() { S = i.ProductId },
                ["ProductName"] = new() { S = i.ProductName },
                ["Quantity"]    = new() { N = i.Quantity.ToString() },
                ["UnitPrice"]   = new() { N = i.UnitPrice.ToString("F2") }
            }
        };

    private static OrderItemDynamoDbModel ToOrderItemModel(AttributeValue v) =>
        new()
        {
            ItemId      = v.M["ItemId"].S,
            ProductId   = v.M["ProductId"].S,
            ProductName = v.M["ProductName"].S,
            Quantity    = int.Parse(v.M["Quantity"].N),
            UnitPrice   = decimal.Parse(v.M["UnitPrice"].N)
        };
}