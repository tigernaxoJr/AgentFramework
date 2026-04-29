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
                    [Embedding] VARBINARY(MAX) NOT NULL
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
                byte[] embeddingBytes = MemoryMarshal.AsBytes(doc.Embedding!.Value.Span).ToArray();

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
                    Embedding = embeddingBytes
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
        
        // 注意：這裡使用簡單的暴力掃描 + 自定義點積運算。
        // 在正式環境中，若 SQL Server 版本支援 VECTOR_DISTANCE，應優先使用。
        // 此範例為了相容性，展示手動計算邏輯。
        
        var rows = await connection.QueryAsync<dynamic>(
            $@"SELECT TOP (@topK) Id, Content, Source, Metadata, Embedding FROM [{_tableName}]",
            new { topK = 1000 } // 先抓出候選集 (在此範例為簡化，實際應使用專用向量函式)
        );

        var results = new List<(Document Doc, float Similarity)>();

        foreach (var row in rows)
        {
            byte[] dbEmbeddingBytes = (byte[])row.Embedding;
            var dbVector = MemoryMarshal.Cast<byte, float>(dbEmbeddingBytes);

            float similarity = CosineSimilarity(queryVector.Span, dbVector);
            
            var doc = new Document
            {
                Id = row.Id,
                Content = row.Content,
                Source = row.Source,
                Embedding = new ReadOnlyMemory<float>(dbVector.ToArray())
            };

            if (!string.IsNullOrEmpty(row.Metadata))
            {
                doc.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata) ?? new Dictionary<string, object>();
            }

            results.Add((doc, similarity));
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .Select(r => r.Doc);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        string sql = $"DELETE FROM [{_tableName}] WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = documentId });
    }

    /// <inheritdoc />
    public Task OptimizeAsync(CancellationToken cancellationToken = default)
    {
        // SQL Server 的索引維護通常由 DBCC 或維護計畫執行，此處暫不做特定操作
        return Task.CompletedTask;
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
