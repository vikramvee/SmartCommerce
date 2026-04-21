using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartCommerce.IntegrationTests.Infrastructure;

public sealed class TestFixture : IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = default!;
    public IAmazonDynamoDB DynamoDb { get; private set; } = default!;
    public IAmazonSQS Sqs { get; private set; } = default!;

    private WebApplicationFactory<Program> _factory = default!;

    // Explicit fake credentials — prevents SDK from hunting for real AWS creds
    private static readonly BasicAWSCredentials FakeCredentials = new("local", "local");

    public async Task InitializeAsync()
    {
        _factory   = new WebApplicationFactory<Program>();
        HttpClient = _factory.CreateClient();

        DynamoDb = new AmazonDynamoDBClient(
        FakeCredentials,
        new AmazonDynamoDBConfig
        {
            ServiceURL        = IntegrationTestSettings.DynamoDbEndpoint,
            UseHttp           = true,
            AuthenticationRegion = "us-east-1"
        });

        Sqs = new AmazonSQSClient(
            FakeCredentials,
            new AmazonSQSConfig
            {
                ServiceURL           = IntegrationTestSettings.LocalStackEndpoint,
                UseHttp              = true,
                AuthenticationRegion = "us-east-1"
            });

                await Task.CompletedTask;
            }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        DynamoDb.Dispose();
        Sqs.Dispose();
        await _factory.DisposeAsync();
    }
}