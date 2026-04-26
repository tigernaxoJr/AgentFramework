using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義資料檢索管線介面，負責查詢轉換、文件檢索、以及重新排序。
/// </summary>
public interface IRetrievalPipeline
{
    /// <summary>
    /// 執行檢索管線。
    /// </summary>
    /// <param name="query">使用者的查詢字串。</param>
    /// <param name="topK">要回傳的最相關文件數量。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>檢索與排序後的文件列表。</returns>
    Task<IEnumerable<RankedItem<Document>>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}
