namespace InventoryService.Infrastructure;

public sealed class SqsSettings
{
    public const string SectionName = "Sqs";
    public string InventoryQueueUrl { get; init; } = default!;
    public string ServiceURL { get; init; } = default!;
}