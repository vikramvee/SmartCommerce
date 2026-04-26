using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using OrderService.Application.Interfaces;
using OrderService.Domain.Orders.Events;
using OrderService.Infrastructure.Idempotency;

namespace OrderService.Infrastructure.Sqs;

public sealed class SqsEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SqsSettings _settings;
    private readonly IEventDispatcher _dispatcher;  
    private readonly ILogger<SqsEventConsumer> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SqsEventConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<SqsSettings> settings,
        IEventDispatcher dispatcher,                
        ILogger<SqsEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings.Value;
        _dispatcher   = dispatcher;                  
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SqsEventConsumer started. Polling {QueueUrl}", _settings.OrdersQueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SqsEventConsumer encountered an error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PollAndProcessAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        // Resolve singleton SQS from scope (works for both singleton and scoped)
        var sqs = scope.ServiceProvider.GetRequiredService<IAmazonSQS>();

        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl            = _settings.OrdersQueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds     = 20,          
            MessageSystemAttributeNames = ["All"],
            AttributeNames        = new List<string> { "All" }
        }, ct);

        // Guard against null response or null Messages from LocalStack
        if (response?.Messages is null || response.Messages.Count == 0) return;

        _logger.LogInformation(
            "SqsEventConsumer received {Count} message(s).", response.Messages.Count);

        foreach (var message in response.Messages)
        {
            await ProcessMessageAsync(scope, sqs, message, ct);
        }
    }

    private async Task ProcessMessageAsync(
    IServiceScope scope,
    IAmazonSQS sqs,
    Message message,
    CancellationToken ct)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body, _jsonOptions);
            if (envelope is null)
            {
                _logger.LogWarning("Could not deserialize SNS envelope. MessageId: {Id}", message.MessageId);
                return;
            }

            var eventType = envelope.MessageAttributes
                ?.GetValueOrDefault("EventType")?.Value ?? string.Empty;

            var correlationId = envelope.MessageAttributes
                ?.GetValueOrDefault("CorrelationId")?.Value
                ?? Guid.NewGuid().ToString();

            // Push BEFORE any logging so all lines carry the CorrelationId
            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            {
                var guard = scope.ServiceProvider.GetRequiredService<IdempotencyGuard>();
                if (await guard.IsDuplicateAsync(envelope.MessageId, eventType, ct))
                {
                    // Duplicate — delete from queue and stop
                    await sqs.DeleteMessageAsync(_settings.OrdersQueueUrl, message.ReceiptHandle, ct);
                    return;
                }
                _logger.LogInformation(
                    "Processing SQS message. EventType: {EventType}, SNS MessageId: {MessageId}",
                    eventType, envelope.MessageId);

            // Add:
            await _dispatcher.DispatchAsync(eventType, envelope.Message, scope, ct);
            await sqs.DeleteMessageAsync(_settings.OrdersQueueUrl, message.ReceiptHandle, ct);

            _logger.LogInformation(
                "SQS message processed and deleted. EventType: {EventType}", eventType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process SQS message {MessageId}. Will retry.", message.MessageId);
        }
    }    
}