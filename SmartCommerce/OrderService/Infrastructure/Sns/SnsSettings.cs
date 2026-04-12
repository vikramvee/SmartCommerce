namespace OrderService.Infrastructure.Sns;

public sealed class SnsSettings
{
    public const string SectionName = "Sns";
    public string OrdersTopicArn { get; init; } = default!;
    public string? ServiceURL { get; init; }
}