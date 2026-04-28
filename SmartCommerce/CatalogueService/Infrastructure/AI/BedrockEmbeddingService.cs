using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace CatalogueService.Infrastructure.AI;

public sealed class BedrockEmbeddingService : IEmbeddingService
{
    private const string ModelId = "amazon.titan-embed-text-v2:0";
    private readonly IAmazonBedrockRuntime _bedrock;

    public BedrockEmbeddingService(IAmazonBedrockRuntime bedrock)
        => _bedrock = bedrock;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { inputText = text });

        var request = new InvokeModelRequest
        {
            ModelId     = ModelId,
            ContentType = "application/json",
            Accept      = "application/json",
            Body        = new MemoryStream(Encoding.UTF8.GetBytes(body))
        };

        var response = await _bedrock.InvokeModelAsync(request, ct);
        var doc      = await JsonDocument.ParseAsync(response.Body, cancellationToken: ct);

        return doc.RootElement
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }
}