namespace MyRAG.Core.Models;

/// <summary>
/// 文本分塊的設定選項。
/// </summary>
public record TextChunkingOptions
{
    /// <summary>
    /// 每行允許的最大 Token 數量。
    /// </summary>
    public int MaxTokensPerLine { get; init; } = 100;

    /// <summary>
    /// 每個段落 (Chunk) 允許的最大 Token 數量。
    /// </summary>
    public int MaxTokensPerParagraph { get; init; } = 256;

    /// <summary>
    /// 相鄰分塊之間重疊的 Token 數量。
    /// </summary>
    public int OverlapTokens { get; init; } = 50;

    /// <summary>
    /// 每個批次處理允許的最大 Token 總數。
    /// </summary>
    public int MaxTokensPerBatch { get; init; } = 8191;

    /// <summary>
    /// 每個批次處理允許的最大項目數量。
    /// </summary>
    public int MaxItemsPerBatch { get; init; } = 16;

    /// <summary>
    /// 語意相似度分塊的門檻值 (僅用於 SemanticChunking)。
    /// </summary>
    public double SemanticSimilarityThreshold { get; init; } = 0.8;

    /// <summary>
    /// 用於計算 Token 數量的模型名稱。
    /// </summary>
    public string TokenizerModelName { get; init; } = "text-embedding-ada-002";
}
