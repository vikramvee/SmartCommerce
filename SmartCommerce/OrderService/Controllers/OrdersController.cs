using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    // GET /api/orders/health-check
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

    // POST /api/orders  ← full implementation comes in Week 2
    [HttpPost]
    public IActionResult PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        _logger.LogInformation(
            "Order received for tenant {TenantId}, customer {CustomerId}",
            request.TenantId,
            request.CustomerId);

        // Week 2: wire up PlaceOrderCommand + DynamoDB + Outbox
        return Accepted(new
        {
            Message    = "Order received — processing",
            TenantId   = request.TenantId,
            CustomerId = request.CustomerId,
        });
    }
}

public record PlaceOrderRequest(
    string TenantId,
    string CustomerId,
    decimal TotalAmount);