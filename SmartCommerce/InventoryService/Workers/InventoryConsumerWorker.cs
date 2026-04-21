using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using InventoryService.Handlers;
using InventoryService.Infrastructure;
using Microsoft.Extensions.Options;
using SmartCommerce.Contracts.Events;

namespace InventoryService.Workers;

public sealed class InventoryConsumerWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsSettings _settings;
    private readonly InventoryReservationHandler _handler;
    private readonly ILogger<InventoryConsumerWorker> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InventoryConsumerWorker(
        IAmazonSQS sqs,
        IOptions<SqsSettings> settings,
        InventoryReservationHandler handler,
        ILogger<InventoryConsumerWorker> logger)
    {
        _sqs      = sqs;
        _settings = settings.Value;
        _handler  = handler;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "InventoryConsumerWorker started. Polling {QueueUrl}",
            _settings.InventoryQueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InventoryConsumerWorker error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PollAndProcessAsync(CancellationToken ct)
    {
        var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl              = _settings.InventoryQueueUrl,
            MaxNumberOfMessages   = 10,
            WaitTimeSeconds       = 20,
            MessageAttributeNames = ["All"],
            AttributeNames        = ["All"]
        }, ct);

        if (response?.Messages is null || response.Messages.Count == 0) return;

        _logger.LogInformation(
            "InventoryConsumerWorker received {Count} message(s).",
            response.Messages.Count);

        foreach (var message in response.Messages)
            await ProcessMessageAsync(message, ct);
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body, _jsonOptions);
            if (envelope is null) return;

            var eventType = envelope.MessageAttributes
                ?.GetValueOrDefault("EventType")?.Value ?? string.Empty;

            var correlationId = envelope.MessageAttributes
                ?.GetValueOrDefault("CorrelationId")?.Value
                ?? Guid.NewGuid().ToString();

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation(
                    "InventoryService processing event: {EventType}", eventType);

                if (eventType == "order.placed")
                {
                    var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(
                        envelope.Message, _jsonOptions);

                    if (orderPlaced is not null)
                        await _handler.HandleOrderPlacedAsync(orderPlaced, ct);
                }

                await _sqs.DeleteMessageAsync(
                    _settings.InventoryQueueUrl, message.ReceiptHandle, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process inventory message {MessageId}.", message.MessageId);
        }
    }
}