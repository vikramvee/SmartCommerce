namespace OrderService.Domain.Common;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();

    public void StampCorrelationId(string correlationId)
    {
        for (int i = 0; i < _domainEvents.Count; i++)
        {
            if (_domainEvents[i] is DomainEvent e)
                _domainEvents[i] = e with { CorrelationId = correlationId };
        }
    }
}