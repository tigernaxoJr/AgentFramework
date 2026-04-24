using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Chunking;
using MyRAG.Core.Embeddings;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Ranking;
using MyRAG.Core.Models;

namespace MyRAG.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering MyRAG services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MyRAG core services to the specified IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configureOptions">An optional action to configure TextChunkingOptions.</param>
    /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
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
}
