using Microsoft.Extensions.AI;

namespace MyRAG.Core.Embeddings;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    /// <summary>
    /// Generates embeddings for a list of text chunks.
    /// </summary>
    public async Task<List<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default)
    {
        var result = await _generator.GenerateAsync(chunks, cancellationToken: cancellationToken);
        return result.ToList();
    }

    /// <summary>
    /// Generates embeddings for multiple batches of text chunks.
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
