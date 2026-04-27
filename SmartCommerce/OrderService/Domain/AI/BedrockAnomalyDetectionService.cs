using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Options;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.AI;

public sealed class BedrockAnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IAmazonBedrockRuntime _bedrock;
    private readonly IOptions<BedrockOptions> _options;
    private readonly ILogger<BedrockAnomalyDetectionService> _logger;

    public BedrockAnomalyDetectionService(
        IAmazonBedrockRuntime bedrock,
        IOptions<BedrockOptions> options,
        ILogger<BedrockAnomalyDetectionService> logger)
    {
        _bedrock = bedrock;
        _options = options;
        _logger = logger;
    }

    public Task<AnomalyResult> DetectAsync(
    string tenantId, decimal totalAmount, int totalQuantity, CancellationToken ct = default)
    {
        var isAnomalous = totalAmount > 10_000 || totalQuantity > 50;
        return Task.FromResult(new AnomalyResult(
            isAnomalous,
            isAnomalous ? "Stub: high-value or high-volume order" : "Stub: order looks normal",
            isAnomalous ? 0.85f : 0.1f));
    }

   private static string BuildPrompt(Order order) => $$"""
    You are an order fraud and anomaly detection system.
    Analyse the following order and respond ONLY in this JSON format:
    {"is_anomalous": true/false, "reason": "...", "confidence": 0.0-1.0}

    Order details:
    - OrderId: {{order.OrderId}}
    - TenantId: {{order.TenantId}}
    - TotalAmount: {{order.Total}}
    - ItemCount: {{order.Items.Count}}
    - Items: {{string.Join(", ", order.Items.Select(i => $"{i.Quantity}x {i.ProductId} @ {i.UnitPrice}"))}}
    - CreatedAt: {{order.CreatedAt:O}}
    """;

    private static AnomalyResult ParseResponse(string text)
    {
        try
        {
            var doc = JsonDocument.Parse(text);
            return new AnomalyResult(
                doc.RootElement.GetProperty("is_anomalous").GetBoolean(),
                doc.RootElement.GetProperty("reason").GetString() ?? "Unknown",
                doc.RootElement.GetProperty("confidence").GetSingle());
        }
        catch
        {
            return new AnomalyResult(false, "Parse error — defaulting to safe", 0f);
        }
    }
}