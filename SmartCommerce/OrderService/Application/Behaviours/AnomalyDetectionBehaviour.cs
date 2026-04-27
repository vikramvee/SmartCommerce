using MediatR;
using OrderService.Application.Commands;

public sealed class AnomalyDetectionBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IOrderCommand
{
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly ILogger<AnomalyDetectionBehaviour<TRequest, TResponse>> _logger;

    public AnomalyDetectionBehaviour(
        IAnomalyDetectionService anomalyService,
        ILogger<AnomalyDetectionBehaviour<TRequest, TResponse>> logger)
    {
        _anomalyService = anomalyService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
    TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is PlaceOrderCommand cmd)
        {
            var totalAmount = cmd.Items.Sum(i => i.Quantity * i.UnitPrice);
            var totalQuantity = cmd.Items.Sum(i => i.Quantity);

            var result = await _anomalyService.DetectAsync(
                cmd.TenantId, totalAmount, totalQuantity, ct);

            if (result.IsAnomalous)
            {
                _logger.LogWarning(
                    "Anomaly detected for tenant {TenantId} — {Reason} (confidence: {Score:P0})",
                    cmd.TenantId, result.Reason, result.ConfidenceScore);
            }
        }

        return await next();
    }
}

