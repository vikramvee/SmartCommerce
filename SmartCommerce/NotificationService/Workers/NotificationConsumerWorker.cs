using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NotificationService.Handlers;
using NotificationService.Infrastructure;
using SmartCommerce.Contracts.Events;

namespace NotificationService.Workers;

public sealed class NotificationConsumerWorker : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsSettings _settings;
    private readonly OrderNotificationHandler _handler;
    private readonly ILogger<NotificationConsumerWorker> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationConsumerWorker(
        IAmazonSQS sqs,
        IOptions<SqsSettings> settings,
        OrderNotificationHandler handler,
        ILogger<NotificationConsumerWorker> logger)
    {
        _sqs      = sqs;
        _settings = settings.Value;
        _handler  = handler;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NotificationConsumerWorker started. Polling {QueueUrl}",
            _settings.NotificationsQueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationConsumerWorker error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PollAndProcessAsync(CancellationToken ct)
    {
        var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl            = _settings.NotificationsQueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds     = 20,
            MessageAttributeNames = ["All"],
            AttributeNames        = ["All"]
        }, ct);

        if (response?.Messages is null || response.Messages.Count == 0) return;

        _logger.LogInformation(
            "NotificationConsumerWorker received {Count} message(s).",
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
                    "NotificationService processing event: {EventType}", eventType);

                if (eventType == "order.placed")
                {
                    var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(
                        envelope.Message, _jsonOptions);

                    if (orderPlaced is not null)
                        await _handler.HandleOrderPlacedAsync(orderPlaced, ct);
                }

                await _sqs.DeleteMessageAsync(
                    _settings.NotificationsQueueUrl, message.ReceiptHandle, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process notification message {MessageId}.", message.MessageId);
        }
    }
}