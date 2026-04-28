namespace CatalogueService.Infrastructure.AI;

public static class CosineSimilarity
{
    public static float Compute(float[] a, float[] b)
    {
        var dot    = a.Zip(b, (x, y) => x * y).Sum();
        var normA  = MathF.Sqrt(a.Sum(x => x * x));
        var normB  = MathF.Sqrt(b.Sum(x => x * x));
        return normA == 0 || normB == 0 ? 0f : dot / (normA * normB);
    }
}