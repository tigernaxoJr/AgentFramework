using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義資料匯入管線介面，負責將文件從來源讀取、切塊、並儲存到向量資料庫中。
/// </summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// 執行資料匯入管線。
    /// </summary>
    /// <param name="source">文件來源 (例如檔案路徑或 URL)。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    Task IngestAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// 將已存在的文件集合直接透過管線處理。
    /// </summary>
    /// <param name="documents">文件集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    Task IngestAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
}
