using OrderService.Domain.Entities;

public sealed class StubAnomalyDetectionService : IAnomalyDetectionService
{
    public Task<AnomalyResult> DetectAsync(
    string tenantId, decimal totalAmount, int totalQuantity, CancellationToken ct = default)
    {
        var isAnomalous = totalAmount > 10_000 || totalQuantity > 50;
        return Task.FromResult(new AnomalyResult(
            isAnomalous,
            isAnomalous ? "Stub: high-value or high-volume order" : "Stub: order looks normal",
            isAnomalous ? 0.85f : 0.1f));
    }
}