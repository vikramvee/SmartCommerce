namespace InventoryService.Infrastructure;

public sealed class SnsEnvelope
{
    public string Type { get; init; } = default!;
    public string MessageId { get; init; } = default!;
    public string Message { get; init; } = default!;
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; init; }
}

public sealed class SnsMessageAttribute
{
    public string Type { get; init; } = default!;
    public string Value { get; init; } = default!;
}