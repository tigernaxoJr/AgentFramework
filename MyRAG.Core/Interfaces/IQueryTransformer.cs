namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義查詢轉換服務，用於將使用者的原始查詢轉換為更高品質的檢索查詢。
/// </summary>
public interface IQueryTransformer
{
    /// <summary>
    /// 轉換輸入的查詢。
    /// </summary>
    /// <param name="query">原始使用者查詢。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>轉換和優化後的查詢字串。</returns>
    Task<string> TransformAsync(string query, CancellationToken cancellationToken = default);
}
