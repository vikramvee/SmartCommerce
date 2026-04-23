using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Outbox;
using OrderService.Infrastructure.Tenancy;


namespace OrderService.Infrastructure.DynamoDB;

public sealed class DynamoDbOrderRepository : IOrderRepository
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly DynamoDbSettings _settings;

    public DynamoDbOrderRepository(IAmazonDynamoDB dynamo, IOptions<DynamoDbSettings> settings)
    {
        _dynamo   = dynamo;
        _settings = settings.Value;
    }

    public async Task<Order?> GetByIdAsync(string tenantId, string orderId, CancellationToken ct = default)
    {
        var response = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _settings.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"TENANT#{tenantId}" },
                ["SK"] = new AttributeValue { S = $"ORDER#{orderId}" }
            }
        }, ct);

        if (!response.IsItemSet) return null;

        var model = AttributeValueMapper.ToOrderModel(response.Item);
        var order = OrderMapper.ToDomain(model);

        // ← Defence-in-depth: reject if tenant mismatch (should never happen,
        //   but guards against PK construction bugs)
        if (!string.Equals(order.TenantId, tenantId, StringComparison.Ordinal))
            throw new TenantIsolationException(tenantId, order.TenantId);

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName              = _settings.TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"]       = new AttributeValue { S = $"TENANT#{tenantId}" },
                [":skPrefix"] = new AttributeValue { S = "ORDER#" }
            }
        }, ct);

        return response.Items
            .Select(item => OrderMapper.ToDomain(AttributeValueMapper.ToOrderModel(item)))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(string tenantId, OrderStatus status, CancellationToken ct = default)
    {
        var response = await _dynamo.QueryAsync(new QueryRequest
        {
            TableName              = _settings.TableName,
            IndexName              = "GSI1",
            KeyConditionExpression = "GSI1PK = :gsi1pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":gsi1pk"] = new AttributeValue { S = $"TENANT#{tenantId}#STATUS#{status}" }
            }
        }, ct);

        return response.Items
            .Select(item => OrderMapper.ToDomain(AttributeValueMapper.ToOrderModel(item)))
            .ToList()
            .AsReadOnly();
    }

    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var orderModel   = OrderMapper.ToModel(order);
        var orderItem    = AttributeValueMapper.ToAttributeMap(orderModel);

        var transactItems = new List<TransactWriteItem>
        {
            new() { Put = new Put { TableName = _settings.TableName, Item = orderItem } }
        };

        // Outbox: write each domain event atomically with the order
        foreach (var domainEvent in order.DomainEvents)
        {
            var outboxMessage = OutboxMessage.Create(
                order.TenantId,
                domainEvent.EventType,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                domainEvent.CorrelationId
            );

            var outboxModel = OutboxMapper.ToModel(outboxMessage);
            var outboxItem  = AttributeValueMapper.ToOutboxAttributeMap(outboxModel);

            transactItems.Add(new TransactWriteItem
            {
                Put = new Put { TableName = _settings.OutboxTableName, Item = outboxItem }
            });
        }

        await _dynamo.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = transactItems
        }, ct);

        // Events safely handed off to Outbox — clear them
        order.ClearDomainEvents();
    }
}