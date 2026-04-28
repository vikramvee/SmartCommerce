using CatalogueService.Domain.Entities;
using CatalogueService.Infrastructure.AI;
using CatalogueService.Infrastructure.DynamoDB;
using MediatR;

namespace CatalogueService.Application.Commands;

public sealed class SeedCatalogueCommandHandler
    : IRequestHandler<SeedCatalogueCommand, SeedCatalogueResult>
{
    private readonly IProductRepository _repository;
    private readonly IEmbeddingService  _embeddings;

    public SeedCatalogueCommandHandler(
        IProductRepository repository,
        IEmbeddingService embeddings)
    {
        _repository = repository;
        _embeddings = embeddings;
    }

    public async Task<SeedCatalogueResult> Handle(
        SeedCatalogueCommand request, CancellationToken ct)
    {
        var products = GetSeedProducts(request.TenantId);

        foreach (var product in products)
        {
            // Generate embedding from name + description
            var text      = $"{product.Name} {product.Description} {product.Category}";
            var embedding = await _embeddings.GenerateEmbeddingAsync(text, ct);

            // Replace with:
            var enriched = new Product
            {
                ProductId   = product.ProductId,
                TenantId    = product.TenantId,
                Name        = product.Name,
                Description = product.Description,
                Category    = product.Category,
                Price       = product.Price,
                CreatedAt   = product.CreatedAt,
                Embedding   = embedding
            };
            await _repository.SaveAsync(enriched, ct);
        }

        return new SeedCatalogueResult(products.Count);
    }

    private static List<Product> GetSeedProducts(string tenantId) =>
    [
        new Product
        {
            ProductId   = Guid.NewGuid().ToString(),
            TenantId    = tenantId,
            Name        = "Laptop Pro X",
            Description = "High performance laptop for developers and creators",
            Category    = "Electronics",
            Price       = 1299.99m,
            CreatedAt   = DateTime.UtcNow
        },
        new Product
        {
            ProductId   = Guid.NewGuid().ToString(),
            TenantId    = tenantId,
            Name        = "Gaming Laptop Ultra",
            Description = "RTX 4080 gaming laptop with 165Hz display",
            Category    = "Electronics",
            Price       = 1899.99m,
            CreatedAt   = DateTime.UtcNow
        },
        new Product
        {
            ProductId   = Guid.NewGuid().ToString(),
            TenantId    = tenantId,
            Name        = "Budget Office Laptop",
            Description = "Lightweight affordable laptop for office productivity",
            Category    = "Electronics",
            Price       = 449.99m,
            CreatedAt   = DateTime.UtcNow
        },
        new Product
        {
            ProductId   = Guid.NewGuid().ToString(),
            TenantId    = tenantId,
            Name        = "Wireless Mechanical Keyboard",
            Description = "Compact TKL mechanical keyboard with RGB backlighting",
            Category    = "Accessories",
            Price       = 129.99m,
            CreatedAt   = DateTime.UtcNow
        },
        new Product
        {
            ProductId   = Guid.NewGuid().ToString(),
            TenantId    = tenantId,
            Name        = "4K Ultra Wide Monitor",
            Description = "34 inch curved ultrawide monitor for productivity",
            Category    = "Displays",
            Price       = 699.99m,
            CreatedAt   = DateTime.UtcNow
        }
    ];
}