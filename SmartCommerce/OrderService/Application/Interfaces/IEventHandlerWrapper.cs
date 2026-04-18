namespace OrderService.Application.Interfaces;

public interface IEventHandlerWrapper
{
    Task HandleAsync(string payload, IServiceScope scope, CancellationToken ct);
}