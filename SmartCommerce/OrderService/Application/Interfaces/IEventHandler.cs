using OrderService.Domain.Common;

namespace OrderService.Application.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}