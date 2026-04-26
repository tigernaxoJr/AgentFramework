using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;

namespace MyRAG.Core.Embeddings;

/// <summary>
/// 提供生成文本向量嵌入的服務實作。
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    /// <summary>
    /// 為提供的文本分塊列表生成向量嵌入。
    /// </summary>
    public async Task<List<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default)
    {
        var result = await _generator.GenerateAsync(chunks, cancellationToken: cancellationToken);
        return result.ToList();
    }

    /// <summary>
    /// 為批次處理的文本分塊生成向量嵌入。
    /// </summary>
    public async Task<List<List<Embedding<float>>>> GenerateBatchedEmbeddingsAsync(IEnumerable<IEnumerable<string>> batchedChunks, CancellationToken cancellationToken = default)
    {
        var allEmbeddings = new List<List<Embedding<float>>>();
        foreach (var batch in batchedChunks)
        {
            var embeddings = await GenerateEmbeddingsAsync(batch, cancellationToken);
            allEmbeddings.Add(embeddings);
        }
        return allEmbeddings;
    }
}
