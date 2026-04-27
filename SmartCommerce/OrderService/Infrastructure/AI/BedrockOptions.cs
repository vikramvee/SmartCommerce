namespace OrderService.Infrastructure.AI;

public sealed class BedrockOptions
{
    public string ModelId { get; set; } = "anthropic.claude-3-haiku-20240307-v1:0";
    public bool UseStub { get; set; } = true;
}