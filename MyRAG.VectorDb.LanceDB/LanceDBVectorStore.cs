using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using lancedb;
using System.Text.Json;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace MyRAG.VectorDb.LanceDB;

/// <summary>
/// IVectorStore 的 LanceDB 實作。
/// </summary>
public class LanceDBVectorStore : IVectorStore
{
    private readonly IEmbeddingService _embeddingService;
    private readonly string _dbPath;
    private readonly string _tableName;
    private lancedb.Connection? _connection;
    private lancedb.Table? _table;

    public LanceDBVectorStore(IEmbeddingService embeddingService, string dbPath, string tableName = "documents")
    {
        _embeddingService = embeddingService;
        _dbPath = dbPath;
        _tableName = tableName;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_connection == null)
        {
            _connection = new lancedb.Connection();
            await _connection.Connect(_dbPath);
        }

        if (_table == null)
        {
            var tableNames = await _connection.TableNames();
            if (tableNames.Contains(_tableName))
            {
                _table = await _connection.OpenTable(_tableName);
            }
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var docList = documents.ToList();
        
        // 確保所有文件都有 Embedding
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

        var batch = ToRecordBatch(docList);

        if (_table == null)
        {
            _table = await _connection!.CreateTable(_tableName, batch);
        }
        else
        {
            await _table.Add(new[] { batch });
        }
    }

    private RecordBatch ToRecordBatch(List<Document> documents)
    {
        if (documents.Count == 0) throw new ArgumentException("Documents list is empty.");

        var firstEmbedding = documents.First().Embedding!.Value;
        int dimension = firstEmbedding.Length;

        // 定義 Schema
        var schemaBuilder = new Schema.Builder()
            .Field(new Field("id", StringType.Default, nullable: false))
            .Field(new Field("content", StringType.Default, nullable: false))
            .Field(new Field("source", StringType.Default, nullable: true))
            .Field(new Field("metadata", StringType.Default, nullable: true));

        var vectorField = new Field("item", FloatType.Default, nullable: false);
        var vectorType = new FixedSizeListType(vectorField, dimension);
        schemaBuilder.Field(new Field("vector", vectorType, nullable: false));

        var schema = schemaBuilder.Build();

        // 建立各個欄位的 Array
        var idBuilder = new StringArray.Builder();
        var contentBuilder = new StringArray.Builder();
        var sourceBuilder = new StringArray.Builder();
        var metadataBuilder = new StringArray.Builder();
        
        // Vector Array 比較特別，需要使用 FixedSizeListArray
        var vectorValueBuilder = new FloatArray.Builder();
        
        foreach (var doc in documents)
        {
            idBuilder.Append(doc.Id);
            contentBuilder.Append(doc.Content);
            sourceBuilder.Append(doc.Source);
            metadataBuilder.Append(JsonSerializer.Serialize(doc.Metadata));

            var vec = doc.Embedding!.Value.Span;
            for (int i = 0; i < dimension; i++)
            {
                vectorValueBuilder.Append(vec[i]);
            }
        }

        var idArray = idBuilder.Build();
        var contentArray = contentBuilder.Build();
        var sourceArray = sourceBuilder.Build();
        var metadataArray = metadataBuilder.Build();
        
        var vectorValues = vectorValueBuilder.Build();
        var vectorArray = new FixedSizeListArray(vectorType, documents.Count, vectorValues, ArrowBuffer.Empty);

        return new RecordBatch(schema, new IArrowArray[] { idArray, contentArray, sourceArray, metadataArray, vectorArray }, documents.Count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        if (_table == null) return Enumerable.Empty<Document>();

        // 產生查詢的向量
        var queryEmbeddingResult = await _embeddingService.GenerateEmbeddingsAsync(new[] { query }, cancellationToken);
        if (queryEmbeddingResult.Count == 0) return Enumerable.Empty<Document>();
        
        var queryVector = queryEmbeddingResult[0].Vector.ToArray().Select(v => (double)v).ToArray();

        // 執行向量搜尋
        var results = await _table.Query()
            .NearestTo(queryVector)
            .Limit(topK)
            .ToList();

        var documents = new List<Document>();
        foreach (var row in results)
        {
            var doc = new Document
            {
                Id = row["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Content = row["content"]?.ToString() ?? string.Empty,
                Source = row["source"]?.ToString(),
                // 將 double[] 轉回 float[]
                Embedding = new ReadOnlyMemory<float>(((double[])row["vector"]!).Select(v => (float)v).ToArray())
            };

            var metadataJson = row["metadata"]?.ToString();
            if (!string.IsNullOrEmpty(metadataJson))
            {
                try 
                {
                    doc.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson) ?? new();
                }
                catch { /* Ignore parsing errors */ }
            }

            documents.Add(doc);
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        if (_table == null) return;

        await _table.Delete($"id = '{documentId}'");
    }
}
