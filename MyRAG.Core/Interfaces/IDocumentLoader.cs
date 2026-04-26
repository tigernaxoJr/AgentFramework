using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義文件讀取器介面，用於從不同來源載入文件。
/// </summary>
public interface IDocumentLoader
{
    /// <summary>
    /// 從指定的來源讀取文件。
    /// </summary>
    /// <param name="source">文件來源 (例如檔案路徑或 URL)。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>讀取到的文件集合。</returns>
    IAsyncEnumerable<Document> LoadAsync(string source, CancellationToken cancellationToken = default);
}
