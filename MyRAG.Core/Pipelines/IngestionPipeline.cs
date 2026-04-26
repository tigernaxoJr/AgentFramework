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
    public async Task IngestAsync(string source, CancellationToken cancellationToken = default)
    {
        if (_documentLoader == null)
            throw new InvalidOperationException("未註冊 IDocumentLoader，無法從來源字串讀取文件。");

        var documents = new List<Document>();
        await foreach (var doc in _documentLoader.LoadAsync(source, cancellationToken))
        {
            documents.Add(doc);
        }

        await IngestAsync(documents, cancellationToken);
    }

    /// <inheritdoc />
    public async Task IngestAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        var chunkedDocuments = new List<Document>();

        foreach (var doc in documents)
        {
            // 將內容切分成批次片段
            var batches = _textChunker.CreateBatchedChunks(doc.Content);
            int index = 0;
            
            foreach (var batch in batches)
            {
                foreach (var chunk in batch)
                {
                    // 為每個 Chunk 建立新的 Document，並繼承原本的 Metadata
                    var metadata = new Dictionary<string, object>(doc.Metadata ?? new Dictionary<string, object>())
                    {
                        { "source_id", doc.Id },
                        { "chunk_index", index }
                    };

                    chunkedDocuments.Add(new Document
                    {
                        Id = $"{doc.Id}_chunk_{index++}",
                        Content = chunk,
                        Metadata = metadata
                    });
                }
            }
        }
        
        // 儲存至向量資料庫
        await _vectorStore.UpsertAsync(chunkedDocuments, cancellationToken);
    }
}
