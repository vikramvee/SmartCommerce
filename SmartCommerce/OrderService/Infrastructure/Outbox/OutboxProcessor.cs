using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using OrderService.Application.Interfaces;
using OrderService.Domain.Common;
using OrderService.Infrastructure.DynamoDB;

namespace OrderService.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DynamoDbSettings _settings;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<DynamoDbSettings> settings,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor encountered an error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        using var scope    = _scopeFactory.CreateScope();
        var dynamo         = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName              = _settings.OutboxTableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "OUTBOX#UNPUBLISHED" }
            },
            // Oldest first — ensures FIFO processing order per tenant
            ScanIndexForward = true,
            Limit = 10
        }, ct);

        if (response.Items.Count == 0) return;

        _logger.LogInformation(
            "OutboxProcessor found {Count} message(s) to process.",
            response.Items.Count);

        foreach (var item in response.Items)
        {
            await ProcessMessageAsync(dynamo, eventPublisher, item, ct);
        }
    }

    private async Task ProcessMessageAsync(
        IAmazonDynamoDB dynamo,
        IEventPublisher publisher,
        Dictionary<string, AttributeValue> item,
        CancellationToken ct)
    {
        var messageId = item["MessageId"].S;
        var eventType = item["EventType"].S;
        var payload   = item["Payload"].S;
        var sk        = item["SK"].S;
        var tenantId  = item["TenantId"].S;
        var createdAt = item["CreatedAt"].S;

        var correlationId = item.TryGetValue("CorrelationId", out var cid)
                    ? cid.S : Guid.NewGuid().ToString();

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            try
            {
                var domainEvent = DeserializeEvent(eventType, payload);
                if (domainEvent is not null)
                {
                    await publisher.PublishAsync(domainEvent, ct);
                }

                await MarkProcessedAsync(dynamo, messageId, tenantId, sk, createdAt, ct);

                _logger.LogInformation(
                    "Outbox message {MessageId} ({EventType}) published successfully.",
                    messageId, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {MessageId} ({EventType}).",
                    messageId, eventType);
            }
        }
    }

    private async Task MarkProcessedAsync(
        IAmazonDynamoDB dynamo,
        string messageId,
        string tenantId,
        string sk,
        string createdAt,
        CancellationToken ct)
    {
        var processedAt = DateTime.UtcNow.ToString("O");

        // Build the PUBLISHED item cleanly — don't copy stale attributes
        var publishedItem = new Dictionary<string, AttributeValue>
        {
            ["PK"]          = new AttributeValue { S = "OUTBOX#PUBLISHED" },
            ["SK"]          = new AttributeValue { S = sk },
            ["GSI1PK"]      = new AttributeValue { S = $"TENANT#{tenantId}" },
            ["GSI1SK"]      = new AttributeValue { S = $"OUTBOX#{messageId}" },
            ["MessageId"]   = new AttributeValue { S = messageId },
            ["TenantId"]    = new AttributeValue { S = tenantId },
            ["CreatedAt"]   = new AttributeValue { S = createdAt },
            ["ProcessedAt"] = new AttributeValue { S = processedAt },
            ["EntityType"]  = new AttributeValue { S = "OUTBOX" }
        };

        await dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems =
            [
                new() { Delete = new Delete
                {
                    TableName = _settings.OutboxTableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = "OUTBOX#UNPUBLISHED" },
                        ["SK"] = new AttributeValue { S = sk }
                    }
                }},
                new() { Put = new Put
                {
                    TableName = _settings.OutboxTableName,
                    Item      = publishedItem
                }}
            ]
        }, ct);
    }

    private static IDomainEvent? DeserializeEvent(string eventType, string payload)
    {
        return eventType switch
        {
            "order.placed"    => JsonSerializer.Deserialize<OrderService.Domain.Orders.Events.OrderPlacedEvent>(payload),
            "order.cancelled" => JsonSerializer.Deserialize<OrderService.Domain.Orders.Events.OrderCancelledEvent>(payload),
            _                 => null
        };
    }
}