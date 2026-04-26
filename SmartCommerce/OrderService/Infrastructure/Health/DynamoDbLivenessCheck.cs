using Amazon.DynamoDBv2;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OrderService.Infrastructure.Health;

public sealed class DynamoDbLivenessCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamo;

    public DynamoDbLivenessCheck(IAmazonDynamoDB dynamo) => _dynamo = dynamo;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dynamo.ListTablesAsync(cancellationToken);
            return HealthCheckResult.Healthy("DynamoDB reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DynamoDB unreachable", ex);
        }
    }
}