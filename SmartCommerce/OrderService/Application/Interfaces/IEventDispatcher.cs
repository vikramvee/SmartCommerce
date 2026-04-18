namespace OrderService.Application.Interfaces;

public interface IEventDispatcher
{
    Task DispatchAsync(string eventType, string payload, IServiceScope scope, CancellationToken ct);
}