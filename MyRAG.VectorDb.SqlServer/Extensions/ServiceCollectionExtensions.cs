using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Interfaces;
using MyRAG.VectorDb.SqlServer;

namespace MyRAG.VectorDb.SqlServer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 註冊 SQL Server 向量資料庫實作。
    /// </summary>
    /// <param name="services">服務集合。</param>
    /// <param name="connectionString">SQL Server 連線字串。</param>
    /// <param name="tableName">資料表名稱 (選填)。</param>
    /// <returns>服務集合。</returns>
    public static IServiceCollection AddSqlServerVectorStore(this IServiceCollection services, string connectionString, string tableName = "VectorDocuments")
    {
        services.AddSingleton<IVectorStore>(sp =>
        {
            var embeddingService = sp.GetRequiredService<IEmbeddingService>();
            return new SqlServerVectorStore(embeddingService, connectionString, tableName);
        });

        return services;
    }
}
