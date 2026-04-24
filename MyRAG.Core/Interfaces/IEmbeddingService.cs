using Microsoft.Extensions.AI;

namespace MyRAG.Core.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generates embeddings for a list of text chunks.
    /// </summary>
    Task<List<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple batches of text chunks.
    /// </summary>
    Task<List<List<Embedding<float>>>> GenerateBatchedEmbeddingsAsync(IEnumerable<IEnumerable<string>> batchedChunks, CancellationToken cancellationToken = default);
}
