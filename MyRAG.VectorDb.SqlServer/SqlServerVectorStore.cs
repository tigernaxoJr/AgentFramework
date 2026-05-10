using Dapper;
using Microsoft.Data.SqlClient;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MyRAG.VectorDb.SqlServer;

/// <summary>
/// IVectorStore 的 SQL Server 實作。
/// 使用 VARBINARY 儲存向量，並透過 T-SQL 計算餘弦相似度。
/// </summary>
public class SqlServerVectorStore : IVectorStore
{
    private readonly IEmbeddingService _embeddingService;
    private readonly string _connectionString;
    private readonly string _tableName;

    public SqlServerVectorStore(IEmbeddingService embeddingService, string connectionString, string tableName = "VectorDocuments")
    {
        _embeddingService = embeddingService;
        _connectionString = connectionString;
        _tableName = tableName;
    }

    private async Task EnsureTableCreatedAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        string sql = $@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{_tableName}')
            BEGIN
                CREATE TABLE [{_tableName}] (
                    [Id] NVARCHAR(450) PRIMARY KEY,
                    [Content] NVARCHAR(MAX) NOT NULL,
                    [Source] NVARCHAR(MAX) NULL,
                    [Metadata] NVARCHAR(MAX) NULL,
                    [Embedding] VECTOR(1024) NOT NULL -- 使用原生向量類型
                );
                CREATE INDEX IX_{_tableName}_Source ON [{_tableName}] ([Source]);
            END";
        await connection.ExecuteAsync(sql);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default)
    {
        await EnsureTableCreatedAsync();

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

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var doc in docList)
            {
                // 將向量轉為 JSON 陣列字串，以相容 SQL Server 的 VECTOR 類型
                var embeddingArray = doc.Embedding!.Value.ToArray();
                string embeddingJson = "[" + string.Join(",", embeddingArray) + "]";

                string sql = $@"
                    IF EXISTS (SELECT 1 FROM [{_tableName}] WHERE Id = @Id)
                    BEGIN
                        UPDATE [{_tableName}] 
                        SET Content = @Content, Source = @Source, Metadata = @Metadata, Embedding = @Embedding
                        WHERE Id = @Id
                    END
                    ELSE
                    BEGIN
                        INSERT INTO [{_tableName}] (Id, Content, Source, Metadata, Embedding)
                        VALUES (@Id, @Content, @Source, @Metadata, @Embedding)
                    END";

                await connection.ExecuteAsync(sql, new
                {
                    doc.Id,
                    doc.Content,
                    doc.Source,
                    Metadata = JsonSerializer.Serialize(doc.Metadata),
                    Embedding = embeddingJson // 傳送 JSON 字串
                }, transaction);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        await EnsureTableCreatedAsync();

        // 產生查詢的向量
        var queryEmbeddingResult = await _embeddingService.GenerateEmbeddingsAsync(new[] { query }, cancellationToken);
        if (queryEmbeddingResult.Count == 0) return Enumerable.Empty<Document>();

        var queryVector = queryEmbeddingResult[0].Vector;
        byte[] queryVectorBytes = MemoryMarshal.AsBytes(queryVector.Span).ToArray();

        using var connection = new SqlConnection(_connectionString);

        // 使用 SQL Server 2025+ 原生向量搜尋函數 VECTOR_DISTANCE
        // 注意：這裡假設資料表已使用 VECTOR 類型。
        // 如果是舊版 VARBINARY，請改用原有的暴力掃描邏輯。
        string sql = $@"
            SELECT TOP (@topK) 
                Id, Content, Source, Metadata, 
                CAST(Embedding AS NVARCHAR(MAX)) as EmbeddingJson,
                VECTOR_DISTANCE('cosine', Embedding, CAST(@queryVector AS VECTOR(1024))) as Distance
            FROM [{_tableName}]
            ORDER BY Distance ASC";

        var queryVectorArray = queryVector.ToArray();
        string queryVectorJson = "[" + string.Join(",", queryVectorArray) + "]";

        var rows = await connection.QueryAsync<dynamic>(sql, new { topK, queryVector = queryVectorJson });

        return rows.Select(row =>
        {
            var doc = new Document
            {
                Id = row.Id,
                Content = row.Content,
                Source = row.Source
            };

            if (!string.IsNullOrEmpty(row.Metadata))
            {
                doc.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata) ?? new Dictionary<string, object>();
            }

            // 解析回來的向量 (如果是從 JSON 轉回來的)
            if (row.EmbeddingJson != null)
            {
                var vector = JsonSerializer.Deserialize<float[]>(row.EmbeddingJson);
                doc.Embedding = new ReadOnlyMemory<float>(vector);
            }

            return doc;
        });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        string sql = $"DELETE FROM [{_tableName}] WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = documentId });
    }

    /// <inheritdoc />
    public async Task OptimizeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. 重新整理索引 (解決刪除與新增導致的碎片化)
        string sql = $@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = '{_tableName}')
            BEGIN
                ALTER INDEX ALL ON [{_tableName}] REORGANIZE;
            END";
        await connection.ExecuteAsync(sql);

        // 2. 收縮資料庫以物理釋放空間回作業系統
        await connection.ExecuteAsync("DBCC SHRINKDATABASE(0);");
    }

    private static float CosineSimilarity(ReadOnlySpan<float> vec1, ReadOnlySpan<float> vec2)
    {
        if (vec1.Length != vec2.Length) return 0;

        float dotProduct = 0;
        float norm1 = 0;
        float norm2 = 0;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            norm1 += vec1[i] * vec1[i];
            norm2 += vec2[i] * vec2[i];
        }

        if (norm1 == 0 || norm2 == 0) return 0;

        return dotProduct / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
    }
}
