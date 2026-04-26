using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;

namespace MyRAG.Core.Pipelines;

/// <summary>
/// 資料檢索管線的具體實作。
/// </summary>
public class RetrievalPipeline : IRetrievalPipeline
{
    private readonly IVectorStore _vectorStore;
    private readonly IQueryTransformer? _queryTransformer;
    private readonly IRankFusion? _rankFusion;
    private readonly IReranker? _reranker;

    public RetrievalPipeline(
        IVectorStore vectorStore, 
        IQueryTransformer? queryTransformer = null, 
        IRankFusion? rankFusion = null,
        IReranker? reranker = null)
    {
        _vectorStore = vectorStore;
        _queryTransformer = queryTransformer;
        _rankFusion = rankFusion;
        _reranker = reranker;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RankedItem<Document>>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queries = new List<string> { query };

        // 1. Query Transformation
        if (_queryTransformer != null)
        {
            var transformed = await _queryTransformer.TransformAsync(query, cancellationToken);
            if (!string.IsNullOrWhiteSpace(transformed) && transformed != query)
            {
                // 如果我們同時想搜尋原始與轉換後的查詢，也可以將兩者都加入
                queries.Add(transformed);
            }
        }

        // 2. Retrieval
        var retrievedLists = new List<IEnumerable<Document>>();
        foreach (var q in queries)
        {
            // 由於可能存在 Fusion，預先檢索較多結果 (例如 topK * 2) 以提高命中率
            var results = await _vectorStore.SearchAsync(q, topK * 2, cancellationToken);
            retrievedLists.Add(results);
        }

        IEnumerable<RankedItem<Document>> rankedResults;

        // 3. Rank Fusion (如果有設定且有多個查詢結果)
        if (_rankFusion != null && retrievedLists.Count > 1)
        {
            rankedResults = _rankFusion.Fuse(retrievedLists, topK * 2);
        }
        else
        {
            // 若無融合，則將第一組結果直接轉換為 RankedItem
            rankedResults = retrievedLists.First().Select((doc, index) => new RankedItem<Document>(doc, 1.0 / (index + 1)));
        }

        // 4. Reranking (重新排序)
        if (_reranker != null)
        {
            var documentsToRerank = rankedResults.Select(r => r.Item);
            rankedResults = await _reranker.RerankAsync(query, documentsToRerank, cancellationToken);
        }

        // 最後擷取前 topK 筆回傳
        return rankedResults.OrderByDescending(r => r.Score).Take(topK);
    }
}
