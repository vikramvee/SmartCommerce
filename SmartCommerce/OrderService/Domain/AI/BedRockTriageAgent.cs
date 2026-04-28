using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Logging;
using OrderService.Domain.AI;

namespace OrderService.Infrastructure.AI;

public sealed class BedrockOrderTriageAgent : IOrderTriageAgent
{
    private const string ModelId = "anthropic.claude-3-haiku-20240307-v1:0";
    private readonly IAmazonBedrockRuntime _bedrock;
    private readonly ILogger<BedrockOrderTriageAgent> _logger;

    public BedrockOrderTriageAgent(
        IAmazonBedrockRuntime bedrock,
        ILogger<BedrockOrderTriageAgent> logger)
    {
        _bedrock = bedrock;
        _logger  = logger;
    }

    public async Task<TriageDecision> TriageAsync(
        string orderId, string tenantId, decimal totalAmount,
        int itemCount, string anomalyReason, CancellationToken ct = default)
    {
        var prompt = $$"""
            You are an AI order triage agent for an e-commerce platform.
            An order has been flagged as potentially anomalous.
            
            Order details:
            - OrderId: {{orderId}}
            - TenantId: {{tenantId}}
            - TotalAmount: {{totalAmount:C}}
            - ItemCount: {{itemCount}}
            - AnomalyReason: {{anomalyReason}}
            
            Based on this information, decide what action to take.
            Respond ONLY in this JSON format, no other text:
            {"action": "Approve|Escalate|Reject", "reasoning": "...", "confidence": 0.0-1.0}
            
            Rules:
            - Approve: anomaly is likely a false positive, order looks legitimate
            - Escalate: suspicious but needs human review
            - Reject: clear fraud indicators, reject immediately
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens        = 256,
            messages          = new[] { new { role = "user", content = prompt } }
        });

        var request = new InvokeModelRequest
        {
            ModelId     = ModelId,
            ContentType = "application/json",
            Accept      = "application/json",
            Body        = new MemoryStream(Encoding.UTF8.GetBytes(requestBody))
        };

        var response     = await _bedrock.InvokeModelAsync(request, ct);
        var responseBody = await JsonDocument.ParseAsync(response.Body, cancellationToken: ct);
        var text         = responseBody.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return ParseDecision(orderId, tenantId, text);
    }

    private static TriageDecision ParseDecision(string orderId, string tenantId, string text)
    {
        try
        {
            var doc    = JsonDocument.Parse(text);
            var action = Enum.Parse<TriageAction>(
                doc.RootElement.GetProperty("action").GetString()!, ignoreCase: true);

            return new TriageDecision(
                orderId, tenantId, action,
                doc.RootElement.GetProperty("reasoning").GetString() ?? string.Empty,
                doc.RootElement.GetProperty("confidence").GetSingle());
        }
        catch
        {
            return new TriageDecision(orderId, tenantId, TriageAction.Escalate,
                "Parse error — defaulting to escalate", 0f);
        }
    }
}