using CatalogueService.Domain.Entities;

namespace CatalogueService.Infrastructure.DynamoDB;

public interface IProductRepository
{
    Task SaveAsync(Product product, CancellationToken ct = default);
    Task<List<Product>> GetAllAsync(string tenantId, CancellationToken ct = default);
}