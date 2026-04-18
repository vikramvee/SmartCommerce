using OrderService.Application.Interfaces;

namespace OrderService.Application.Dispatching;

public sealed class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<string, IEventHandlerWrapper> _registry;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(
        IEnumerable<KeyValuePair<string, IEventHandlerWrapper>> registrations,
        ILogger<EventDispatcher> logger)
    {
        _registry = registrations.ToDictionary(r => r.Key, r => r.Value);
        _logger   = logger;
    }

    public async Task DispatchAsync(
        string eventType,
        string payload,
        IServiceScope scope,
        CancellationToken ct)
    {
        if (!_registry.TryGetValue(eventType, out var wrapper))
        {
            _logger.LogWarning("No handler registered for event type: {EventType}", eventType);
            return;
        }

        await wrapper.HandleAsync(payload, scope, ct);
    }
}