using CatalogueService.Infrastructure.AI;
using CatalogueService.Infrastructure.DynamoDB;
using MediatR;

namespace CatalogueService.Application.Queries;

public sealed class SemanticSearchQueryHandler
    : IRequestHandler<SemanticSearchQuery, List<SemanticSearchResult>>
{
    private readonly IEmbeddingService _embeddings;
    private readonly IProductRepository _repository;

    public SemanticSearchQueryHandler(
        IEmbeddingService embeddings,
        IProductRepository repository)
    {
        _embeddings = embeddings;
        _repository = repository;
    }

    public async Task<List<SemanticSearchResult>> Handle(
        SemanticSearchQuery request, CancellationToken ct)
    {
        // 1. Embed the search query
        var queryVector = await _embeddings.GenerateEmbeddingAsync(request.QueryText, ct);

        // 2. Fetch all products for tenant (small catalogue — full scan is fine for now)
        var products = await _repository.GetAllAsync(request.TenantId, ct);

        // 3. Rank by cosine similarity
        return products
            .Select(p => new SemanticSearchResult(
                p.ProductId,
                p.Name,
                p.Description,
                p.Price,
                CosineSimilarity.Compute(queryVector, p.Embedding)))
            .OrderByDescending(r => r.Score)
            .Take(request.TopK)
            .ToList();
    }
}