using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Chunking;
using MyRAG.Core.Embeddings;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.Core.Ranking;
using MyRAG.Core.Retrieval;
using MyRAG.Core.Storage;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;
using MyRAG.Core.Pipelines;

namespace MyRAG.Core.DependencyInjection;

/// <summary>
/// 提供擴充方法，用於將 MyRAG 服務註冊到 IServiceCollection 中。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 將 MyRAG 核心服務加入到指定的 IServiceCollection 中。
    /// </summary>
    /// <param name="services">要加入服務的 IServiceCollection。</param>
    /// <param name="configureOptions">可選的委派，用於設定 TextChunkingOptions。</param>
    /// <returns>回傳 IServiceCollection 以支援鏈式呼叫。</returns>
    public static IServiceCollection AddMyRagCore(this IServiceCollection services, Action<TextChunkingOptions>? configureOptions = null)
    {
        var options = new TextChunkingOptions();
        configureOptions?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<ITextChunkingService, SemanticKernelChunker>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IRankFusion, ReciprocalRankFusion>();
        
        return services;
    }

    /// <summary>
    /// 將記憶體內向量資料庫 (In-Memory Vector Store) 加入到服務中。此服務需要先註冊 IEmbeddingService。
    /// </summary>
    public static IServiceCollection AddInMemoryVectorStore(this IServiceCollection services)
    {
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        return services;
    }

    /// <summary>
    /// 將查詢改寫器 (Query Rewriter) 加入到服務中。此服務需要先註冊 IChatClient。
    /// </summary>
    public static IServiceCollection AddQueryRewriter(this IServiceCollection services)
    {
        services.AddSingleton<IQueryTransformer, QueryRewriter>();
        return services;
    }

    /// <summary>
    /// 將 HyDE 轉換器 (HyDE Transformer) 加入到服務中。此服務需要先註冊 IChatClient。
    /// </summary>
    public static IServiceCollection AddHyDETransformer(this IServiceCollection services)
    {
        services.AddSingleton<IQueryTransformer, HyDETransformer>();
        return services;
    }

    /// <summary>
    /// 新增 OpenAI 相容的 Embedding 生成器 (例如：OpenAI, LM Studio, Ollama 等)。
    /// 需與 AddMyRagCore 搭配使用，此方法將註冊 IEmbeddingGenerator 供 EmbeddingService 使用。
    /// </summary>
    /// <param name="services">要加入服務的 IServiceCollection。</param>
    /// <param name="endpoint">API 端點 (例如：http://localhost:1234/v1)。</param>
    /// <param name="apiKey">API 金鑰 (若為本地端相容 API，通常可填任意字串或 "lm-studio")。</param>
    /// <param name="modelId">要使用的模型 ID (例如：text-embedding-nomic-embed-text-v1.5)。</param>
    /// <returns>回傳 IServiceCollection 以支援鏈式呼叫。</returns>
    public static IServiceCollection AddOpenAICompatibleEmbeddingGenerator(this IServiceCollection services, string endpoint, string apiKey, string modelId)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            return new OpenAICompatibleEmbeddingGenerator(modelId, apiKey, endpoint);
        });

        return services;
    }

    /// <summary>
    /// 將 RAG Pipelines 與 RagEngine 加入到服務中。
    /// 建議在註冊完 ITextChunkingService, IVectorStore, 以及任何需要的 Transformer / Reranker 後呼叫此方法。
    /// </summary>
    public static IServiceCollection AddRagPipelines(this IServiceCollection services)
    {
        services.AddSingleton<IIngestionPipeline, IngestionPipeline>();
        services.AddSingleton<IRetrievalPipeline, RetrievalPipeline>();
        services.AddSingleton<IRagEngine, RagEngine>();
        return services;
    }
}
