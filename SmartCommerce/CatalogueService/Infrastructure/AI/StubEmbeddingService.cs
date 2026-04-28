namespace CatalogueService.Infrastructure.AI;

/// <summary>
/// Returns a deterministic fake embedding for offline dev.
/// Same input always produces same vector so cosine similarity still works.
/// </summary>
public sealed class StubEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 1536;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var rng = new Random(text.GetHashCode());
        var embedding = Enumerable.Range(0, Dimensions)
            .Select(_ => (float)rng.NextDouble())
            .ToArray();

        return Task.FromResult(embedding);
    }
}