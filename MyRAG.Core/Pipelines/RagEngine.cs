using MyRAG.Core.Interfaces;

namespace MyRAG.Core.Pipelines;

/// <summary>
/// RAG 引擎的預設實作，提供對資料匯入管線與檢索管線的集中存取。
/// </summary>
public class RagEngine : IRagEngine
{
    public RagEngine(IIngestionPipeline ingestion, IRetrievalPipeline retrieval)
    {
        Ingestion = ingestion;
        Retrieval = retrieval;
    }

    /// <inheritdoc />
    public IIngestionPipeline Ingestion { get; }

    /// <inheritdoc />
    public IRetrievalPipeline Retrieval { get; }
}
