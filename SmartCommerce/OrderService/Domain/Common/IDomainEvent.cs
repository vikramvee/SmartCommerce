namespace OrderService.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
    string CorrelationId { get; }  // ← add this
}