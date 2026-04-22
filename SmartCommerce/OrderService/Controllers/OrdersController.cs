using Microsoft.AspNetCore.Mvc;
using MediatR;
using OrderService.Application.Commands;
using OrderService.Infrastructure.Tenancy;

namespace OrderService.Controllers;

[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    private readonly ITenantContext _tenantContext;  

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _logger   = logger;
        _tenantContext = tenantContext;
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
}