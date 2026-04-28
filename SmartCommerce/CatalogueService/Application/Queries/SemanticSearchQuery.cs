using MediatR;

namespace CatalogueService.Application.Queries;

public sealed record SemanticSearchQuery(
    string TenantId,
    string QueryText,
    int TopK = 5
) : IRequest<List<SemanticSearchResult>>;

public sealed record SemanticSearchResult(
    string ProductId,
    string Name,
    string Description,
    decimal Price,
    float Score
);