using OrderService.Domain.AI;

namespace OrderService.Infrastructure.AI;

public sealed class StubOrderTriageAgent : IOrderTriageAgent
{
    public Task<TriageDecision> TriageAsync(
        string orderId, string tenantId, decimal totalAmount,
        int itemCount, string anomalyReason, CancellationToken ct = default)
    {
        var (action, reasoning) = totalAmount switch
        {
            > 50_000 => (TriageAction.Reject,    "Stub: extremely high value — auto reject"),
            > 10_000 => (TriageAction.Escalate,  "Stub: high value order — needs human review"),
            _        => (TriageAction.Approve,    "Stub: within acceptable range — auto approve")
        };

        return Task.FromResult(new TriageDecision(
            orderId, tenantId, action, reasoning, 0.9f));
    }
}