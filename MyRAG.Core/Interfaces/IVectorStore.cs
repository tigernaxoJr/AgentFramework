using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義向量資料庫的儲存與檢索介面。
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// 新增或更新文件及其向量表示。
    /// </summary>
    /// <param name="documents">要儲存的文件集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    Task UpsertAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據查詢字串執行相似度搜尋。
    /// </summary>
    /// <param name="query">搜尋查詢字串。</param>
    /// <param name="topK">要回傳的最相關文件數量 (預設為 5)。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>檢索到的相關文件列表。</returns>
    Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據文件 ID 刪除指定文件。
    /// </summary>
    /// <param name="documentId">要刪除的文件 ID。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    Task DeleteAsync(string documentId, CancellationToken cancellationToken = default);
}
