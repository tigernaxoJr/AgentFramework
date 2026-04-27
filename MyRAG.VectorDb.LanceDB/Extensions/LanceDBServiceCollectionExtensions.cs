using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Interfaces;

namespace MyRAG.VectorDb.LanceDB.Extensions;

/// <summary>
/// 提供 LanceDB 向量資料庫的相依注入擴充方法。
/// </summary>
public static class LanceDBServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 LanceDB 向量資料庫服務。
    /// </summary>
    /// <param name="services">服務集合。</param>
    /// <param name="dbPath">資料庫儲存路徑。</param>
    /// <param name="tableName">資料表名稱 (預設為 "documents")。</param>
    /// <returns>服務集合。</returns>
    public static IServiceCollection AddLanceDBVectorStore(this IServiceCollection services, string dbPath, string tableName = "documents")
    {
        services.AddSingleton<IVectorStore>(sp => 
        {
            var embeddingService = sp.GetRequiredService<IEmbeddingService>();
            return new LanceDBVectorStore(embeddingService, dbPath, tableName);
        });
        
        return services;
    }
}
