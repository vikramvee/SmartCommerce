using OrderService.Domain.Common;

namespace OrderService.Domain.Events;

public sealed record OrderAnomalyDetectedEvent : DomainEvent, ITenantEvent
{
    public override string EventType    => "order.anomaly.detected";
    public string OrderId      { get; init; } = default!;
    public string TenantId     { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public int ItemCount       { get; init; }
    public string AnomalyReason { get; init; } = default!;
    public float Confidence    { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;   
}