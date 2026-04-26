namespace MyRAG.Core.Models;

/// <summary>
/// 代表一個可以被索引和檢索的文本文件。
/// </summary>
public class Document
{
    /// <summary>
    /// 文件的唯一識別碼。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 文件的主要文本內容。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 附加的文件中繼資料 (例如：標題、作者、建立時間等)。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 文件的來源 (例如：檔案路徑、URL)。
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// 文件的向量嵌入表示。
    /// </summary>
    public ReadOnlyMemory<float>? Embedding { get; set; }
}
