namespace MyRAG.Core.Models;

/// <summary>
/// 指定文本切塊的策略。
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// 基於固定長度並帶有重疊的批次切塊 (預設)。
    /// </summary>
    Batched,
    
    /// <summary>
    /// 基於語意分析的切塊，透過計算句子間的向量相似度來尋找切分點。
    /// 需要配置 IEmbeddingGenerator 才能使用。
    /// </summary>
    Semantic
}
