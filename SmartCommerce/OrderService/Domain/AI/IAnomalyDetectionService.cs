using OrderService.Domain.Entities;

public record AnomalyResult(
    bool IsAnomalous,
    string Reason,
    float ConfidenceScore);

public interface IAnomalyDetectionService
{
    Task<AnomalyResult> DetectAsync(
        string tenantId, decimal totalAmount, int totalQuantity, CancellationToken ct = default);
}