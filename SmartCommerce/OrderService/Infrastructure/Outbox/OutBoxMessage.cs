using Amazon.DynamoDBv2.DataModel;

namespace OrderService.Infrastructure.Outbox;

// Outbox rows sit in the SAME DynamoDB table
// PK = OUTBOX#{messageId}
// SK = PENDING  (changes to PROCESSED after publish)

[DynamoDBTable("SmartCommerce")]
public class OutboxMessage
{
    [DynamoDBHashKey("PK")]
    public string PK { get; set; } = default!;

    [DynamoDBRangeKey("SK")]
    public string SK { get; set; } = "PENDING";

    public string MessageId { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;   // JSON
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }

    public static OutboxMessage Create(string tenantId, string eventType, string payload) =>
        new()
        {
            MessageId = Guid.NewGuid().ToString(),
            EventType = eventType,
            Payload   = payload,
            Status    = "PENDING",
            CreatedAt = DateTime.UtcNow,
            PK        = $"OUTBOX#{Guid.NewGuid()}",
            SK        = "PENDING",
            TenantId = tenantId
        };
}