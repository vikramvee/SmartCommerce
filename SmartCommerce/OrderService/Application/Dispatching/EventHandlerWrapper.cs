using System.Text.Json;
using OrderService.Application.Interfaces;
using OrderService.Domain.Common;

namespace OrderService.Application.Dispatching;

public sealed class EventHandlerWrapper<TEvent> : IEventHandlerWrapper
    where TEvent : class, IDomainEvent
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(string payload, IServiceScope scope, CancellationToken ct)
    {
        var domainEvent = JsonSerializer.Deserialize<TEvent>(payload, _jsonOptions);
        if (domainEvent is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize payload into {typeof(TEvent).Name}.");
        }

        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<TEvent>>();
        await handler.HandleAsync(domainEvent, ct);
    }
}