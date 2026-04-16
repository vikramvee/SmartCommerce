namespace OrderService.Infrastructure.Correlation;

public sealed class CorrelationIdAccessor
{
    public string CorrelationId { get; private set; } = Guid.NewGuid().ToString();

    public void Set(string correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
            CorrelationId = correlationId;
    }
}