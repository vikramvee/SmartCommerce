using MediatR;
using OrderService.Application.Commands;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;
using OrderService.Infrastructure.Correlation;

public sealed class AnomalyDetectionBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IOrderCommand
{
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly IEventPublisher _publisher;
    private readonly CorrelationIdAccessor _correlationId;
    private readonly ILogger<AnomalyDetectionBehaviour<TRequest, TResponse>> _logger;

    public AnomalyDetectionBehaviour(
        IAnomalyDetectionService anomalyService,
        IEventPublisher publisher,
        CorrelationIdAccessor correlationId,
        ILogger<AnomalyDetectionBehaviour<TRequest, TResponse>> logger)
    {
        _anomalyService = anomalyService;
        _publisher      = publisher;
        _correlationId  = correlationId;
        _logger         = logger;
    }

    public async Task<TResponse> Handle(
    TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Let the handler run first so we have an OrderId
        var response = await next();

        if (request is PlaceOrderCommand cmd)
        {
            var totalAmount   = cmd.Items.Sum(i => i.Quantity * i.UnitPrice);
            var totalQuantity = cmd.Items.Sum(i => i.Quantity);

            var result = await _anomalyService.DetectAsync(
                cmd.TenantId, totalAmount, totalQuantity, ct);

            if (result.IsAnomalous)
            {
                // Extract OrderId from the result
                var orderId = response is PlaceOrderResult placeResult 
                    ? placeResult.OrderId 
                    : "unknown";

                _logger.LogWarning(
                    "Anomaly detected for Order {OrderId} tenant {TenantId} — {Reason} (confidence: {Score:P0})",
                    orderId, cmd.TenantId, result.Reason, result.ConfidenceScore);

                await _publisher.PublishAsync(new OrderAnomalyDetectedEvent
                {             
                    CorrelationId = _correlationId.CorrelationId,
                    OrderId       = orderId,
                    TenantId      = cmd.TenantId,
                    TotalAmount   = totalAmount,
                    ItemCount     = totalQuantity,
                    AnomalyReason = result.Reason,
                    Confidence    = result.ConfidenceScore
                }, ct);
            }
        }

        return response;
    }
}