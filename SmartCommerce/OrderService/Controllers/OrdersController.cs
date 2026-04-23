using Microsoft.AspNetCore.Mvc;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Infrastructure.Tenancy;
using OrderService.Application.Interfaces;

namespace OrderService.Controllers;

[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    private readonly ITenantContext _tenantContext; 
    private readonly IOrderRepository _orderRepository; 

    public OrdersController(
        IMediator mediator,
        ILogger<OrdersController> logger,
        ITenantContext tenantContext,
        IOrderRepository orderRepository)              
    {
        _mediator          = mediator;
        _logger            = logger;
        _tenantContext     = tenantContext;
        _orderRepository   = orderRepository;
    }

    [HttpGet("health-check")]
    public IActionResult HealthCheck()
    {
        _logger.LogInformation("OrderService health check called");
        return Ok(new
        {
            Service = "OrderService",
            Status  = "Healthy",
            Time    = DateTime.UtcNow
        });
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            _tenantContext.TenantId,
            request.CustomerId,
            request.Items.Select(i => new PlaceOrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice
            )).ToList()
        );

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(PlaceOrder), new { id = result.OrderId }, result);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId, CancellationToken ct)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(
                _tenantContext.TenantId, orderId, ct);

            if (order is null) return NotFound();

            return Ok(new
            {
                order.OrderId,
                order.TenantId,
                order.CustomerId,
                order.Status,
                order.Total,
                order.CreatedAt
            });
        }
        catch (TenantIsolationException ex)
        {
            _logger.LogCritical(ex,
                "TENANT ISOLATION VIOLATION - TenantId: {TenantId} OrderId: {OrderId}",
                _tenantContext.TenantId, orderId);

            // Return 404 not 403 — don't confirm the resource exists
            return NotFound();
        }
    }
}