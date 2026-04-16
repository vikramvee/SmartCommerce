using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure.Idempotency;

public sealed class IdempotencyGuard
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly IdempotencySettings _settings;
    private readonly ILogger<IdempotencyGuard> _logger;

    public IdempotencyGuard(
        IAmazonDynamoDB dynamo,
        IOptions<IdempotencySettings> settings,
        ILogger<IdempotencyGuard> logger)
    {
        _dynamo   = dynamo;
        _settings = settings.Value;
        _logger   = logger;
    }

    /// <summary>
    /// Returns true if the message is a duplicate and should be skipped.
    /// Returns false if the message is new and has been recorded for processing.
    /// </summary>
    public async Task<bool> IsDuplicateAsync(string messageId, string eventType, CancellationToken ct)
    {
        var pk  = $"MSG#{messageId}";
        var ttl = DateTimeOffset.UtcNow.AddDays(_settings.TtlDays).ToUnixTimeSeconds();

        try
        {
            // Conditional write — only succeeds if PK does not exist
            await _dynamo.PutItemAsync(new PutItemRequest
            {
                TableName           = _settings.TableName,
                ConditionExpression = "attribute_not_exists(PK)",
                Item = new Dictionary<string, AttributeValue>
                {
                    ["PK"]        = new() { S = pk },
                    ["MessageId"] = new() { S = messageId },
                    ["EventType"] = new() { S = eventType },
                    ["ProcessedAt"] = new() { S = DateTime.UtcNow.ToString("O") },
                    ["TTL"]       = new() { N = ttl.ToString() }
                }
            }, ct);

            // Write succeeded — message is new
            return false;
        }
        catch (ConditionalCheckFailedException)
        {
            // PK already exists — duplicate
            _logger.LogWarning(
                "Duplicate message detected and skipped. MessageId: {MessageId}, EventType: {EventType}",
                messageId, eventType);
            return true;
        }
    }
}