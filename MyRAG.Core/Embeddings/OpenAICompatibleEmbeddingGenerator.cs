using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyRAG.Core.Embeddings;

/// <summary>
/// 提供 OpenAI 相容的 Embedding 生成器實作。
/// </summary>
public sealed class OpenAICompatibleEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly EmbeddingClient _client;

    /// <summary>
    /// 使用指定的模型 ID、API 金鑰與端點初始化新的實例。
    /// </summary>
    public OpenAICompatibleEmbeddingGenerator(string modelId, string apiKey, string endpoint)
    {
        _client = new EmbeddingClient(modelId, new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // EmbeddingClient does not implement IDisposable
    }

    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, 
        Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var inputs = values.ToList();
        var embeddings = await _client.GenerateEmbeddingsAsync(inputs, null, cancellationToken);
        
        var result = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var emb in embeddings.Value)
        {
            result.Add(new Embedding<float>(emb.ToFloats()));
        }
        
        return result;
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}
