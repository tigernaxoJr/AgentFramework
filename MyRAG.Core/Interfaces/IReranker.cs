using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義重新排名服務，用於對初步檢索到的文件進行更精確的重新評分與排序。
/// </summary>
public interface IReranker
{
    /// <summary>
    /// 根據提供的查詢對文件集合進行重新排名。
    /// </summary>
    /// <param name="query">使用者的查詢字串。</param>
    /// <param name="documents">初步檢索到的文件集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>重新排名後的文件列表，包含更新後的分數。</returns>
    Task<IEnumerable<RankedItem<Document>>> RerankAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default);
}
