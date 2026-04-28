namespace OrderService.Domain.Events;

public sealed record OrderTriagedEvent(
    string OrderId,
    string TenantId,
    string Action,
    string Reasoning,
    float Confidence,
    DateTime TriagedAt
);