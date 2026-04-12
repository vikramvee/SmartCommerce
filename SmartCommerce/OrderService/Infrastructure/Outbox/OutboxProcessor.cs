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

        // Query unpublished outbox messages
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName              = _settings.OutboxTableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "OUTBOX#UNPUBLISHED" }
            },
            Limit = 10  // process in batches
        }, ct);

        if (response.Items.Count == 0) return;

        _logger.LogInformation("OutboxProcessor found {Count} messages to process.", response.Items.Count);

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

        try
        {
            // Deserialize and publish
            var domainEvent = DeserializeEvent(eventType, payload);
            if (domainEvent is not null)
            {
                await publisher.PublishAsync(domainEvent, ct);
            }

            // Mark as processed — move from UNPUBLISHED to PUBLISHED
            await MarkProcessedAsync(dynamo, item, messageId, tenantId, sk, ct);

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

    private async Task MarkProcessedAsync(
        IAmazonDynamoDB dynamo,
        Dictionary<string, AttributeValue> item,
        string messageId, string tenantId, string sk,
        CancellationToken ct)
    {
        var processedAt = DateTime.UtcNow.ToString("O");

        // Write processed copy
        var processedItem = new Dictionary<string, AttributeValue>(item)
        {
            ["PK"]          = new AttributeValue { S = "OUTBOX#PUBLISHED" },
            ["ProcessedAt"] = new AttributeValue { S = processedAt }
        };

        await dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                // Delete from UNPUBLISHED
                new() { Delete = new Delete
                {
                    TableName = _settings.OutboxTableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = "OUTBOX#UNPUBLISHED" },
                        ["SK"] = new AttributeValue { S = sk }
                    }
                }},
                // Insert into PUBLISHED
                new() { Put = new Put
                {
                    TableName = _settings.OutboxTableName,
                    Item      = processedItem
                }}
            }
        }, ct);
    }

    private static IDomainEvent? DeserializeEvent(string eventType, string payload)
    {
        return eventType switch
        {
            "order.placed"    => JsonSerializer.Deserialize<OrderService.Domain.Orders.Events.OrderPlacedEvent>(payload),
            "order.cancelled" => JsonSerializer.Deserialize<OrderService.Domain.Orders.Events.OrderCancelledEvent>(payload),
            _ => null
        };
    }
}