using System.Text.Json.Serialization;

namespace OrderService.Infrastructure.Sqs;

// SNS wraps the message payload in this envelope when delivering to SQS
public sealed class SnsEnvelope
{
    [JsonPropertyName("Type")]
    public string Type { get; init; } = default!;

    [JsonPropertyName("MessageId")]
    public string MessageId { get; init; } = default!;

    [JsonPropertyName("TopicArn")]
    public string TopicArn { get; init; } = default!;

    [JsonPropertyName("Message")]
    public string Message { get; init; } = default!;  // ← actual event payload

    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; init; }
}

public sealed class SnsMessageAttribute
{
    [JsonPropertyName("Type")]
    public string Type { get; init; } = default!;

    [JsonPropertyName("Value")]
    public string Value { get; init; } = default!;
}