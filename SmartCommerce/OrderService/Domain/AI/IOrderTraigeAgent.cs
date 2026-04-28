namespace OrderService.Domain.AI;

public interface IOrderTriageAgent
{
    Task<TriageDecision> TriageAsync(
        string orderId,
        string tenantId,
        decimal totalAmount,
        int itemCount,
        string anomalyReason,
        CancellationToken ct = default);
}