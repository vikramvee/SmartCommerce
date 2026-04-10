using Amazon.DynamoDBv2.DataModel;
using SmartCommerce.Domain.Entities;


namespace OrderService.Infrastructure.DynamoDB;

// Single-table design:
// PK = TENANT#{tenantId}#ORDER#{orderId}
// SK = METADATA
// GSI1PK = CUSTOMER#{customerId}  ← query by customer
// GSI1SK = ORDER#{orderId}

[DynamoDBTable("SmartCommerce")]
public class OrderDynamoDbModel
{
    [DynamoDBHashKey("PK")]
    public string PK { get; set; } = default!;

    [DynamoDBRangeKey("SK")]
    public string SK { get; set; } = "METADATA";

    [DynamoDBGlobalSecondaryIndexHashKey("GSI1", AttributeName = "GSI1PK")]
    public string GSI1PK { get; set; } = default!;

    [DynamoDBGlobalSecondaryIndexRangeKey("GSI1", AttributeName = "GSI1SK")]
    public string GSI1SK { get; set; } = default!;

    public Guid OrderId { get; set; }
    public string TenantId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Factory — encapsulates key construction
    // public static Order Create(string tenantId, string customerId, decimal total)
    // {
    //     var orderId = Guid.NewGuid();
    //     return new Order
    //     {
    //         OrderId    = orderId,
    //         TenantId   = tenantId,
    //         CustomerId = customerId,
    //         TotalAmount = total,
    //         Status     = OrderStatus.Pending,
    //         CreatedAt  = DateTime.UtcNow,
    //         UpdatedAt  = DateTime.UtcNow,
    //         PK         = $"TENANT#{tenantId}#ORDER#{orderId}",
    //         SK         = "METADATA",
    //         GSI1PK     = $"CUSTOMER#{customerId}",
    //         GSI1SK     = $"ORDER#{orderId}",
    //     };
    // }
}