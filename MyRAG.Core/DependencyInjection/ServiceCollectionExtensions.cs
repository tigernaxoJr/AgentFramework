using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Chunking;
using MyRAG.Core.Embeddings;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Ranking;
using MyRAG.Core.Models;
using MyRAG.Core.Retrieval;
using MyRAG.Core.Storage;

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
}
