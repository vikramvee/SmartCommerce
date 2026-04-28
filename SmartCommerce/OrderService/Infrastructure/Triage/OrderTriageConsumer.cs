using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Domain.AI;
using OrderService.Infrastructure.Sns;
using OrderService.Infrastructure.Sqs;

namespace OrderService.Infrastructure.Triage;

public sealed class OrderTriageConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IOrderTriageAgent _agent;
    private readonly ILogger<OrderTriageConsumer> _logger;
    private readonly string _queueUrl;

    public OrderTriageConsumer(
    IAmazonSQS sqs,
    IOrderTriageAgent agent,
    IOptions<SqsSettings> sqsSettings,
    ILogger<OrderTriageConsumer> logger)
    {
        _sqs      = sqs;
        _agent    = agent;
        _logger   = logger;
        _queueUrl = sqsSettings.Value.TriageQueueUrl
            ?? throw new InvalidOperationException(
                "Sqs:TriageQueueUrl is not configured. Check appsettings.Development.json");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("OrderTriageConsumer started, polling {Queue}", _queueUrl);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl            = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds     = 10
                }, ct);

                if (response?.Messages is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    continue;
                }

                foreach (var message in response.Messages)
                    await ProcessMessageAsync(message, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error polling triage queue");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            var payload = JsonDocument.Parse(message.Body);
            var root    = payload.RootElement;

            // SNS wraps the actual message
            var body = root.TryGetProperty("Message", out var msg)
                ? JsonDocument.Parse(msg.GetString()!).RootElement
                : root;

            // Only process anomaly events
            if (!body.TryGetProperty("EventType", out var eventType) ||
                eventType.GetString() != "order.anomaly.detected")
            {
                await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
                return;
            }

            var orderId       = body.GetProperty("OrderId").GetString()!;
            var tenantId      = body.GetProperty("TenantId").GetString()!;
            var totalAmount   = body.GetProperty("TotalAmount").GetDecimal();
            var itemCount     = body.GetProperty("ItemCount").GetInt32();
            var anomalyReason = body.GetProperty("AnomalyReason").GetString()!;

            _logger.LogInformation(
                "Triaging order {OrderId} for tenant {TenantId}", orderId, tenantId);

            var decision = await _agent.TriageAsync(
                orderId, tenantId, totalAmount, itemCount, anomalyReason, ct);

            _logger.LogInformation(
                "Triage decision for {OrderId}: {Action} — {Reasoning} (confidence: {Score:P0})",
                orderId, decision.Action, decision.Reasoning, decision.Confidence);

            await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process triage message {MessageId}", message.MessageId);
        }
    }
}