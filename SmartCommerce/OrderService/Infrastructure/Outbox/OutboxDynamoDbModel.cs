namespace OrderService.Infrastructure.Outbox;

public sealed class OutboxDynamoDbModel
{
    public string PK { get; set; } = default!;      // OUTBOX#UNPUBLISHED or OUTBOX#PUBLISHED
    public string SK { get; set; } = default!;      // <timestamp>#<messageId>
    public string GSI1PK { get; set; } = default!;  // TENANT#<tenantId>
    public string GSI1SK { get; set; } = default!;  // OUTBOX#<messageId>
    public string MessageId { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
    public string? ProcessedAt { get; set; }
    public string EntityType { get; set; } = "OUTBOX";
    public string CorrelationId { get; set; } = string.Empty;
}