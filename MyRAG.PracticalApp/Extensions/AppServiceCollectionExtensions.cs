using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using MyRAG.Core.DependencyInjection;
using MyRAG.Core.Interfaces;
using MyRAG.Embeddings.Onnx.Extensions;
using MyRAG.Reranking.Onnx.Extensions;
using MyRAG.VectorDb.LanceDB.Extensions;
using MyRAG.VectorDb.SqlServer.Extensions;
using MyRAG.PracticalApp.Configuration;

namespace MyRAG.PracticalApp.Extensions;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddPracticalAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 綁定組態
        var appOptions = new RagAppOptions();
        configuration.Bind(appOptions);
        services.AddSingleton(appOptions);

        // 1. Core MyRAG Services
        services.AddMyRagCore();

        // 2. Configure Vector DB
        if (appOptions.VectorDb.Provider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(appOptions.VectorDb.SqlServer.ConnectionString))
            {
                Console.WriteLine("[Warning] SQL Server 供應商被選取，但未提供 ConnectionString，將回退到 LanceDB。");
                services.AddLanceDBVectorStore(appOptions.VectorDb.LanceDB.Path, appOptions.VectorDb.LanceDB.TableName);
            }
            else
            {
                services.AddSqlServerVectorStore(appOptions.VectorDb.SqlServer.ConnectionString, appOptions.VectorDb.SqlServer.TableName);
                Console.WriteLine("[System] Using SQL Server Vector DB");
            }
        }
        else
        {
            services.AddLanceDBVectorStore(appOptions.VectorDb.LanceDB.Path, appOptions.VectorDb.LanceDB.TableName);
            Console.WriteLine("[System] Using LanceDB Vector DB");
        }

        // 3. Configure Embeddings (ONNX Local Model)
        if (!string.IsNullOrWhiteSpace(appOptions.OnnxEmbedding.ModelPath) && 
            !string.IsNullOrWhiteSpace(appOptions.OnnxEmbedding.TokenizerPath))
        {
            services.AddOnnxEmbeddingGenerator(
                appOptions.OnnxEmbedding.ModelPath,
                appOptions.OnnxEmbedding.TokenizerPath,
                appOptions.OnnxEmbedding.UseGPU
            );
            Console.WriteLine("[System] Configured ONNX Embedding Generator");
        }
        else
        {
            Console.WriteLine("[Warning] ONNX Embedding 模型路徑未設定，請確保已設定或未用到 Embedding。");
        }

        // 4. Configure Reranker (ONNX Local Model) if enabled
        if (appOptions.OnnxReranker.Enabled)
        {
            if (!string.IsNullOrWhiteSpace(appOptions.OnnxReranker.ModelPath) && 
                !string.IsNullOrWhiteSpace(appOptions.OnnxReranker.TokenizerPath))
            {
                services.AddOnnxReranker(
                    appOptions.OnnxReranker.ModelPath,
                    appOptions.OnnxReranker.TokenizerPath,
                    appOptions.OnnxReranker.UseGPU
                );
                Console.WriteLine("[System] Configured ONNX Reranker");
            }
            else
            {
                Console.WriteLine("[Warning] Reranker 已啟用但未設定模型路徑，已自動停用二次排序。");
                appOptions.OnnxReranker.Enabled = false; // 強制關閉避免後續出錯
            }
        }

        // 5. Configure Chat Client for Query Expansion if enabled
        if (appOptions.QueryExpansion.Enabled)
        {
            if (!string.IsNullOrWhiteSpace(appOptions.QueryExpansion.Endpoint) && 
                !string.IsNullOrWhiteSpace(appOptions.QueryExpansion.ModelId))
            {
                // 使用 OpenAI 相容伺服器
                var apiKey = string.IsNullOrWhiteSpace(appOptions.QueryExpansion.ApiKey) 
                             ? "dummy-key" 
                             : appOptions.QueryExpansion.ApiKey;

                var openAiClient = new OpenAI.OpenAIClient(
                    new System.ClientModel.ApiKeyCredential(apiKey), 
                    new OpenAI.OpenAIClientOptions { Endpoint = new Uri(appOptions.QueryExpansion.Endpoint) }
                );
                IChatClient chatClient = openAiClient.GetChatClient(appOptions.QueryExpansion.ModelId).AsIChatClient();

                services.AddSingleton<IChatClient>(chatClient);
                Console.WriteLine($"[System] Configured Query Expansion with OpenAI Compatible Server ({appOptions.QueryExpansion.ModelId})");
            }
            else
            {
                Console.WriteLine("[Warning] Query Expansion 已啟用但未設定 Endpoint 或 ModelId，已自動停用擴充。");
                appOptions.QueryExpansion.Enabled = false;
            }
        }

        // 6. Register RAG Pipelines (IRagEngine)
        services.AddRagPipelines();

        return services;
    }
}
