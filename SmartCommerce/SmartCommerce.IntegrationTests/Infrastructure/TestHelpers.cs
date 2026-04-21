using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;
using System.Net.Http.Json;

namespace SmartCommerce.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    /// <summary>
    /// Polls until condition is true or timeout elapses.
    /// </summary>
    public static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        var delay    = interval ?? TimeSpan.FromSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
            await Task.Delay(delay);
        }

        return false;
    }

    /// <summary>
    /// Checks if an order exists in DynamoDB by OrderId.
    /// </summary>
public static async Task<bool> OrderExistsAsync(
    IAmazonDynamoDB dynamo, string orderId)
{
    var response = await dynamo.QueryAsync(new QueryRequest
    {
        TableName              = IntegrationTestSettings.OrdersTableName,
        KeyConditionExpression = "PK = :pk AND SK = :sk",
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":pk"] = new() { S = $"TENANT#{IntegrationTestSettings.TenantId}" },
            [":sk"] = new() { S = $"ORDER#{orderId}" }
        },
        Limit = 1
    });

    return response.Items.Count > 0;
}
    /// <summary>
    /// Checks if outbox message was published (moved to OUTBOX#PUBLISHED).
    /// </summary>
public static async Task<bool> OutboxPublishedAsync(
    IAmazonDynamoDB dynamo, string orderId)
{
    // Scan OUTBOX#PUBLISHED for the orderId in Payload
    // This is acceptable for tests — not for production
    var response = await dynamo.QueryAsync(new QueryRequest
    {
        TableName              = IntegrationTestSettings.OutboxTableName,
        KeyConditionExpression = "PK = :pk",
        FilterExpression       = "contains(Payload, :orderId)",
        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            [":pk"]      = new() { S = "OUTBOX#PUBLISHED" },
            [":orderId"] = new() { S = orderId }
        }
    });

    return response.Items.Count > 0;
}

    /// <summary>
    /// Checks if a queue received a message containing the orderId.
    /// Does NOT delete the message.
    /// </summary>
    public static async Task<bool> QueueReceivedOrderAsync(
    IAmazonSQS sqs, string queueUrl, string orderId)
    {
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl              = queueUrl,
            MaxNumberOfMessages   = 10,
            WaitTimeSeconds       = 5,
            MessageAttributeNames = ["All"]
        });

        // Guard against null Messages
        if (response?.Messages is null || response.Messages.Count == 0)
            return false;

        return response.Messages.Any(m => m.Body.Contains(orderId));
    }


    public static async Task<string> PlaceOrderAndGetIdAsync(HttpClient client)
    {
        var request = new
        {
            tenantId   = IntegrationTestSettings.TenantId,
            customerId = IntegrationTestSettings.CustomerId,
            items = new[]
            {
                new { productId = "prod-001", productName = "Test Product", quantity = 2, unitPrice = 49.99m }
            }
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);
        var body     = await response.Content.ReadAsStringAsync();
        var result   = System.Text.Json.JsonSerializer.Deserialize<OrderResponse>(
            body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result!.OrderId;
    }
}

public sealed record OrderResponse
{
    public string OrderId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}