using CatalogueService.Application.Commands;
using CatalogueService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CatalogueService.Api;

[ApiController]
[Route("api/catalogue")]
public sealed class CatalogueController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogueController(IMediator mediator)
        => _mediator = mediator;

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SeedCatalogueCommand(tenantId), ct);
        return Ok(new { result.ProductsSeeded, message = "Catalogue seeded successfully" });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId,
        [FromQuery] string q,
        [FromQuery] int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required" });

        var results = await _mediator.Send(
            new SemanticSearchQuery(tenantId, q, topK), ct);

        return Ok(results);
    }
}