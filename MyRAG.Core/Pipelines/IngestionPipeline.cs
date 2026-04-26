using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;

namespace MyRAG.Core.Pipelines;

/// <summary>
/// 資料匯入管線的具體實作。
/// </summary>
public class IngestionPipeline : IIngestionPipeline
{
    private readonly ITextChunkingService _textChunker;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentLoader? _documentLoader;

    public IngestionPipeline(ITextChunkingService textChunker, IVectorStore vectorStore, IDocumentLoader? documentLoader = null)
    {
        _textChunker = textChunker;
        _vectorStore = vectorStore;
        _documentLoader = documentLoader;
    }

    /// <inheritdoc />
    public async Task IngestAsync(string source, ChunkingStrategy chunkingStrategy = ChunkingStrategy.Batched, CancellationToken cancellationToken = default)
    {
        if (_documentLoader == null)
            throw new InvalidOperationException("未註冊 IDocumentLoader，無法從來源字串讀取文件。");

        var documents = new List<Document>();
        await foreach (var doc in _documentLoader.LoadAsync(source, cancellationToken))
        {
            documents.Add(doc);
        }

        await IngestAsync(documents, chunkingStrategy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task IngestAsync(IEnumerable<Document> documents, ChunkingStrategy chunkingStrategy = ChunkingStrategy.Batched, CancellationToken cancellationToken = default)
    {
        var chunkedDocuments = new List<Document>();

        foreach (var doc in documents)
        {
            int index = 0;

            if (chunkingStrategy == ChunkingStrategy.Semantic)
            {
                var semanticChunks = await _textChunker.CreateSemanticChunksAsync(doc.Content, cancellationToken);
                foreach (var chunk in semanticChunks)
                {
                    chunkedDocuments.Add(CreateChunkDocument(doc, chunk, index++));
                }
            }
            else
            {
                var batches = _textChunker.CreateBatchedChunks(doc.Content);
                foreach (var batch in batches)
                {
                    foreach (var chunk in batch)
                    {
                        chunkedDocuments.Add(CreateChunkDocument(doc, chunk, index++));
                    }
                }
            }
        }
        
        // 儲存至向量資料庫
        await _vectorStore.UpsertAsync(chunkedDocuments, cancellationToken);
    }

    private static Document CreateChunkDocument(Document originalDoc, string chunkContent, int chunkIndex)
    {
        var metadata = new Dictionary<string, object>(originalDoc.Metadata ?? new Dictionary<string, object>())
        {
            { "source_id", originalDoc.Id },
            { "chunk_index", chunkIndex }
        };

        return new Document
        {
            Id = $"{originalDoc.Id}_chunk_{chunkIndex}",
            Content = chunkContent,
            Metadata = metadata
        };
    }
}
