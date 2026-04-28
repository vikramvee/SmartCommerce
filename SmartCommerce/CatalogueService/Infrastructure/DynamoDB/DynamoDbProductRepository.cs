using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CatalogueService.Domain.Entities;
using System.Text.Json;

namespace CatalogueService.Infrastructure.DynamoDB;

public sealed class DynamoDbProductRepository : IProductRepository
{
    private const string TableName = "SmartCommerce_Orders";
    private readonly IAmazonDynamoDB _dynamo;

    public DynamoDbProductRepository(IAmazonDynamoDB dynamo)
        => _dynamo = dynamo;

    public async Task SaveAsync(Product product, CancellationToken ct = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"]          = new AttributeValue { S = $"TENANT#{product.TenantId}" },
            ["SK"]          = new AttributeValue { S = $"PRODUCT#{product.ProductId}" },
            ["ProductId"]   = new AttributeValue { S = product.ProductId },
            ["TenantId"]    = new AttributeValue { S = product.TenantId },
            ["Name"]        = new AttributeValue { S = product.Name },
            ["Description"] = new AttributeValue { S = product.Description },
            ["Category"]    = new AttributeValue { S = product.Category },
            ["Price"]       = new AttributeValue { N = product.Price.ToString() },
            ["Embedding"]   = new AttributeValue { S = JsonSerializer.Serialize(product.Embedding) },
            ["CreatedAt"]   = new AttributeValue { S = product.CreatedAt.ToString("O") }
        };

        await _dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item      = item
        }, ct);
    }

    public async Task<List<Product>> GetAllAsync(string tenantId, CancellationToken ct = default)
    {
        var request = new QueryRequest
        {
            TableName              = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"]       = new AttributeValue { S = $"TENANT#{tenantId}" },
                [":skPrefix"] = new AttributeValue { S = "PRODUCT#" }
            }
        };

        var response = await _dynamo.QueryAsync(request, ct);

        return response.Items.Select(item => new Product
        {
            ProductId   = item["ProductId"].S,
            TenantId    = item["TenantId"].S,
            Name        = item["Name"].S,
            Description = item["Description"].S,
            Category    = item["Category"].S,
            Price       = decimal.Parse(item["Price"].N),
            Embedding   = JsonSerializer.Deserialize<float[]>(item["Embedding"].S) ?? [],
            CreatedAt   = DateTime.Parse(item["CreatedAt"].S)
        }).ToList();
    }
}