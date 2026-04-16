

namespace OrderService.Infrastructure.Outbox;

public static class OutboxMapper
{
    public static OutboxDynamoDbModel ToModel(OutboxMessage message)
    {
        var timestamp = message.CreatedAt.ToString("O");

        return new OutboxDynamoDbModel
        {
            PK            = "OUTBOX#UNPUBLISHED",
            SK            = $"{timestamp}#{message.MessageId}",
            GSI1PK        = $"TENANT#{message.TenantId}",
            GSI1SK        = $"OUTBOX#{message.MessageId}",
            MessageId     = message.MessageId,
            TenantId      = message.TenantId,
            EventType     = message.EventType,
            Payload       = message.Payload,
            CreatedAt     = timestamp,
            CorrelationId = message.CorrelationId  // ← add
        };
    }
}