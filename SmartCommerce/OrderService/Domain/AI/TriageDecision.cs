namespace OrderService.Domain.AI;

public enum TriageAction { Approve, Escalate, Reject }

public sealed record TriageDecision(
    string OrderId,
    string TenantId,
    TriageAction Action,
    string Reasoning,
    float Confidence
);