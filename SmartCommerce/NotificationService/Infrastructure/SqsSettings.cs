namespace NotificationService.Infrastructure;

public sealed class SqsSettings
{
    public const string SectionName = "Sqs";
    public string NotificationsQueueUrl { get; init; } = default!;
    public string ServiceURL { get; init; } = default!;
}