namespace CatalogueService.Domain.Entities;

public sealed class Product
{
    public string ProductId { get; init; } = default!;
    public string TenantId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Category { get; init; } = default!;
    public decimal Price { get; init; }
    public float[] Embedding { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}