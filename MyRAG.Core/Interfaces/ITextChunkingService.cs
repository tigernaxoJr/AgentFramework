using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義文本分塊服務，用於將長文本拆分為較小的片段 (Chunks)。
/// </summary>
public interface ITextChunkingService
{
    /// <summary>
    /// 將文本拆分為段落，並根據 Token 限制將它們合併為批次。
    /// </summary>
    /// <param name="text">要分塊的原始文本。</param>
    /// <returns>包含多個批次的列表，每個批次包含多個文本分塊。</returns>
    List<List<string>> CreateBatchedChunks(string text);

    /// <summary>
    /// 根據句子之間的語意相似度拆分文本 (Semantic Chunking)。
    /// </summary>
    /// <param name="text">要分塊的原始文本。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>基於語意拆分的文本分塊列表。</returns>
    Task<List<string>> CreateSemanticChunksAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算給定文本的 Token 數量。
    /// </summary>
    /// <param name="text">要計算的文本。</param>
    /// <returns>Token 數量。</returns>
    int CountTokens(string text);
}
