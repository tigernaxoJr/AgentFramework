namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義 RAG 引擎，整合 Ingestion 與 Retrieval 管線。
/// </summary>
public interface IRagEngine
{
    /// <summary>
    /// 取得資料匯入管線。
    /// </summary>
    IIngestionPipeline Ingestion { get; }

    /// <summary>
    /// 取得資料檢索管線。
    /// </summary>
    IRetrievalPipeline Retrieval { get; }
}
