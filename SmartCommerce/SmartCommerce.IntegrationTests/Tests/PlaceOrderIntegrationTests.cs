using System.Net;
using System.Net.Http.Json;
using SmartCommerce.IntegrationTests.Infrastructure;
using Xunit;

namespace SmartCommerce.IntegrationTests.Tests;

public sealed class PlaceOrderIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public PlaceOrderIntegrationTests(TestFixture fixture)
        => _fixture = fixture;

[Fact]
public async Task PlaceOrder_ShouldSaveOrderToDynamoDB()
{
    // Act
    var orderId = await TestHelpers.PlaceOrderAndGetIdAsync(_fixture.HttpClient);

    // Assert
    Assert.False(string.IsNullOrEmpty(orderId));

    // Assert — Order saved to DynamoDB
    var orderSaved = await TestHelpers.WaitUntilAsync(
        () => TestHelpers.OrderExistsAsync(_fixture.DynamoDb, orderId),
        timeout: TimeSpan.FromSeconds(10));

    Assert.True(orderSaved, $"Order {orderId} was not found in DynamoDB.");
}

  [Fact]
public async Task PlaceOrder_ShouldPublishOutboxMessage()
{
    // Act
    var orderId = await TestHelpers.PlaceOrderAndGetIdAsync(_fixture.HttpClient);

    // Assert — event delivered to orders queue proves outbox published to SNS
    var delivered = await TestHelpers.WaitUntilAsync(
        () => TestHelpers.QueueReceivedOrderAsync(
            _fixture.Sqs,
            "http://localhost:4566/000000000000/smartcommerce-orders-queue",
            orderId),
        timeout: TimeSpan.FromSeconds(30));

    Assert.True(delivered,
        $"Outbox message for order {orderId} was not published to SNS/SQS.");
}

    [Fact]
    public async Task PlaceOrder_ShouldDeliverToNotificationsQueue()
    {
        // Arrange
        var request = BuildPlaceOrderRequest();

        // Act
        var orderId = await TestHelpers.PlaceOrderAndGetIdAsync(_fixture.HttpClient);

        // Assert — message arrives in notifications queue within 30 seconds
        var delivered = await TestHelpers.WaitUntilAsync(
            () => TestHelpers.QueueReceivedOrderAsync(
                _fixture.Sqs,
                IntegrationTestSettings.NotificationsQueueUrl,
                orderId),
            timeout: TimeSpan.FromSeconds(30));

        Assert.True(delivered,
            $"Order {orderId} was not delivered to notifications queue.");
    }

    [Fact]
    public async Task PlaceOrder_ShouldDeliverToInventoryQueue()
    {
        // Arrange
        var request = BuildPlaceOrderRequest();

        // Act
        var orderId = await TestHelpers.PlaceOrderAndGetIdAsync(_fixture.HttpClient);

        // Assert — message arrives in inventory queue within 30 seconds
        var delivered = await TestHelpers.WaitUntilAsync(
            () => TestHelpers.QueueReceivedOrderAsync(
                _fixture.Sqs,
                IntegrationTestSettings.InventoryQueueUrl,
                orderId),
            timeout: TimeSpan.FromSeconds(30));

        Assert.True(delivered,
            $"Order {orderId} was not delivered to inventory queue.");
    }

    [Fact]
    public async Task PlaceOrder_ShouldReturnCorrelationIdHeader()
    {
        // Arrange
        var request = BuildPlaceOrderRequest();

        // Act
        var response = await _fixture.HttpClient
            .PostAsJsonAsync("/api/orders", request);

        // Assert — correlation ID echoed back in response header
        Assert.True(
            response.Headers.Contains("X-Correlation-Id"),
            "Response is missing X-Correlation-Id header.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object BuildPlaceOrderRequest() => new
    {
        tenantId   = IntegrationTestSettings.TenantId,
        customerId = IntegrationTestSettings.CustomerId,
        items = new[]
        {
            new
            {
                productId   = "prod-001",
                productName = "Test Product",
                quantity    = 2,
                unitPrice   = 49.99m
            }
        }
    };

    [Fact]
public async Task Debug_DynamoDbConnection()
{
    var response = await _fixture.DynamoDb.ListTablesAsync();
    Assert.Contains("SmartCommerce_Orders", response.TableNames);
}

[Fact]
public async Task Debug_OrderQuery()
{
    // First place an order
    var request = new
    {
        tenantId   = "tenant-alpha",
        customerId = "customer-001",
        items = new[]
        {
            new { productId = "prod-001", productName = "Test Product", quantity = 2, unitPrice = 49.99m }
        }
    };

    var orderId = await TestHelpers.PlaceOrderAndGetIdAsync(_fixture.HttpClient);

    // Wait 2 seconds for DynamoDB write
    await Task.Delay(2000);

    // Try the exact query
    var result = await _fixture.DynamoDb.QueryAsync(new Amazon.DynamoDBv2.Model.QueryRequest
    {
        TableName              = "SmartCommerce_Orders",
        KeyConditionExpression = "PK = :pk AND SK = :sk",
        ExpressionAttributeValues = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        {
            [":pk"] = new() { S = $"TENANT#tenant-alpha" },
            [":sk"] = new() { S = $"ORDER#{orderId}" }
        }
    });

    Assert.True(result.Count > 0, $"OrderId={orderId}, Count={result.Count}");
}
}

