using OrderService.Application.Interfaces;
using OrderService.Domain.Common;

namespace OrderService.Application.Dispatching;

public sealed class EventDispatcherBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<KeyValuePair<string, IEventHandlerWrapper>> _registrations = [];

    public EventDispatcherBuilder(IServiceCollection services)
        => _services = services;

    public EventDispatcherBuilder Register<TEvent, THandler>(string eventType)
        where TEvent : class, IDomainEvent
        where THandler : class, IEventHandler<TEvent>
    {
        _services.AddScoped<IEventHandler<TEvent>, THandler>();
        _registrations.Add(new KeyValuePair<string, IEventHandlerWrapper>(
            eventType,
            new EventHandlerWrapper<TEvent>()));
        return this;
    }

    public void Build()
    {
        var registrations = _registrations;
        _services.AddSingleton<IEventDispatcher>(sp =>
            new EventDispatcher(
                registrations,
                sp.GetRequiredService<ILogger<EventDispatcher>>()));
    }
}