namespace SmartCommerce.IntegrationTests.Infrastructure;

public static class IntegrationTestSettings
{
    public const string DynamoDbEndpoint        = "http://localhost:8000";
    public const string LocalStackEndpoint      = "http://localhost:4566";
    public const string Region                  = "us-east-1";
    public const string OrdersTableName         = "SmartCommerce_Orders";
    public const string OutboxTableName         = "SmartCommerce_Outbox";
    public const string NotificationsQueueUrl   = "http://localhost:4566/000000000000/smartcommerce-notifications-queue";
    public const string InventoryQueueUrl       = "http://localhost:4566/000000000000/smartcommerce-inventory-queue";
    public const string TenantId                = "tenant-alpha";
    public const string CustomerId              = "customer-001";
}