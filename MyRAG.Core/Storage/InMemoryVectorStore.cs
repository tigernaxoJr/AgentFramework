using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace MyRAG.Core.Storage;

/// <summary>
/// IVectorStore 的簡單記憶體內實作。
/// 適用於測試或小規模應用。
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, Document> _store = new();
    private readonly IEmbeddingService _embeddingService;

    public InMemoryVectorStore(IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        var docList = documents.ToList();
        var docsToEmbed = docList.Where(d => d.Embedding == null || d.Embedding.Value.IsEmpty).ToList();
        
        if (docsToEmbed.Count > 0)
        {
            var contents = docsToEmbed.Select(d => d.Content);
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(contents, cancellationToken);
            
            for (int i = 0; i < docsToEmbed.Count; i++)
            {
                docsToEmbed[i].Embedding = embeddings[i].Vector;
            }
        }

        foreach (var doc in docList)
        {
            _store[doc.Id] = doc;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (_store.IsEmpty) return Enumerable.Empty<Document>();

        // Generate embedding for the search query
        var queryEmbeddingResult = await _embeddingService.GenerateEmbeddingsAsync(new[] { query }, cancellationToken);
        if (queryEmbeddingResult.Count == 0) return Enumerable.Empty<Document>();
        
        var queryVectorMemory = queryEmbeddingResult[0].Vector;

        // Perform brute-force cosine similarity search
        var results = _store.Values
            .Where(d => d.Embedding != null && !d.Embedding.Value.IsEmpty)
            .Select(d => new
            {
                Document = d,
                Similarity = TensorPrimitives.CosineSimilarity(d.Embedding!.Value.Span, queryVectorMemory.Span)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => x.Document);

        return results.ToList();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(documentId, out _);
        return Task.CompletedTask;
    }
}
