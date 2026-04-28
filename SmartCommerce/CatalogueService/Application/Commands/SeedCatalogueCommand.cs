using MediatR;

namespace CatalogueService.Application.Commands;

public sealed record SeedCatalogueCommand(string TenantId) : IRequest<SeedCatalogueResult>;
public sealed record SeedCatalogueResult(int ProductsSeeded);