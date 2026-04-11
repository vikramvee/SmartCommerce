public sealed class DynamoDbSettings
{
    public const string SectionName = "DynamoDB";
    public string TableName { get; init; } = "SmartCommerce_Orders";
    public string OutboxTableName { get; init; } = "SmartCommerce_Outbox";
    public string? ServiceURL { get; init; } // null = real AWS, set for local
}