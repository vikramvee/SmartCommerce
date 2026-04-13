namespace OrderService.Infrastructure.Sqs;

public sealed class SqsSettings
{
    public const string SectionName = "Sqs";
    public string OrdersQueueUrl { get; init; } = default!;
    public string? ServiceURL { get; init; }
}