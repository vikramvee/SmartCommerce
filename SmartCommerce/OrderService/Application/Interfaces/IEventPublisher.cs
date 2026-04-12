using OrderService.Domain.Common;

namespace OrderService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}