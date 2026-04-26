using Microsoft.Extensions.AI;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義生成文本向量嵌入的服務介面。
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 為提供的文本分塊列表生成向量嵌入。
    /// </summary>
    /// <param name="chunks">文本分塊的集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>包含生成向量嵌入的列表。</returns>
    Task<List<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// 為批次處理的文本分塊生成向量嵌入。
    /// </summary>
    /// <param name="batchedChunks">批次處理的文本分塊集合。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>包含每個批次的向量嵌入列表的列表。</returns>
    Task<List<List<Embedding<float>>>> GenerateBatchedEmbeddingsAsync(IEnumerable<IEnumerable<string>> batchedChunks, CancellationToken cancellationToken = default);
}
