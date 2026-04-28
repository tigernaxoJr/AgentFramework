using Microsoft.Extensions.AI;
using MyRAG.Core.Chunking;
using MyRAG.Core.Embeddings;
using MyRAG.Core.Models;
using MyRAG.Core.Retrieval;
using MyRAG.VectorDb.LanceDB;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;

namespace MyRAG.Samples.Samples._06_Advanced_RAG_LanceDB;

/// <summary>
/// 示範進階 RAG 流程：
/// 1. 使用 SemanticKernelChunker 進行 Overlapping Chunking。
/// 2. 使用 LanceDB 作為向量資料庫。
/// 3. 使用 MultiQueryExpander 進行查詢擴展 (Query Expansion)。
/// 4. 最後產出帶有 Context 的 Prompt。
/// </summary>
public class AdvancedRagSample
{
    public static async Task RunAsync()
    {
        // 1. 環境設定 (讀取環境變數，參考 MyGoogle.cs 的設定方式)
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "YOUR_GEMINI_API_KEY";
        var modelId = Environment.GetEnvironmentVariable("OPENAI_API_MODEL") ?? "gemini-1.5-flash";
        var embeddingModelId = Environment.GetEnvironmentVariable("OPENAI_API_EMBEDDING_MODEL") ?? "text-embedding-004";
        var endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/v1/";

        Console.WriteLine("=== 進階 RAG 範例程式 (LanceDB + Overlap + Query Expansion) ===\n");

        // 2. 初始化元件
        // LLM Chat Client (用於 Query Expansion)
        var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions 
        { 
            Endpoint = new Uri(endpoint) 
        });
        var chatClient = openAIClient.GetChatClient(modelId).AsIChatClient();

        // Embedding Generator & Service
        var embeddingGenerator = new OpenAICompatibleEmbeddingGenerator(embeddingModelId, apiKey, endpoint);
        var embeddingService = new EmbeddingService(embeddingGenerator);

        // Chunker (設定重疊切塊)
        var chunkerOptions = new TextChunkingOptions
        {
            MaxTokensPerParagraph = 300,
            OverlapTokens = 100 // 保留 100 tokens 的重疊內容
        };
        var chunker = new SemanticKernelChunker(chunkerOptions);

        // LanceDB Vector Store (指定資料庫路徑與資料表名稱)
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lancedb_advanced");
        var vectorStore = new LanceDBVectorStore(embeddingService, dbPath, "wiki_docs");

        // 3. 資料準備與匯入 (Ingestion Pipeline)
        Console.WriteLine("[1/3] 正在進行資料匯入 (Ingestion) 與重疊切塊...");
        
        var rawDocuments = GetSampleData();
        foreach (var rawDoc in rawDocuments)
        {
            // 執行切塊 (此步驟會應用 OverlapTokens)
            var textChunks = chunker.CreateBatchedChunks(rawDoc.Content).SelectMany(batch => batch).ToList();
            
            var documentsToUpsert = textChunks.Select(chunk => new Document
            {
                Id = Guid.NewGuid().ToString(),
                Content = chunk,
                Source = rawDoc.Title,
                Metadata = new Dictionary<string, object> 
                { 
                    { "category", "technical_doc" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            });

            // 存入 LanceDB (內部會自動呼叫 Embedding Service 生成向量)
            await vectorStore.UpsertAsync(documentsToUpsert);
            Console.WriteLine($" - 已匯入文件: {rawDoc.Title} ({textChunks.Count()} 區塊)");
        }

        // 4. 查詢擴展與檢索 (Retrieval Pipeline)
        Console.WriteLine("\n[2/3] 正在執行查詢擴展 (Query Expansion) 與向量檢索...");
        
        string userQuery = "LanceDB 有什麼特點？它如何處理大數據？";
        Console.WriteLine($"原始查詢: {userQuery}");

        var expander = new MultiQueryExpander(chatClient, numQueries: 2);
        var expandedQueries = await expander.ExpandAsync(userQuery);
        
        Console.WriteLine("擴展後的查詢:");
        foreach (var q in expandedQueries) Console.WriteLine($" -> {q}");

        // 針對每個擴展查詢進行檢索，並合併結果
        var allResults = new List<Document>();
        foreach (var q in expandedQueries)
        {
            var results = await vectorStore.SearchAsync(q, topK: 3);
            allResults.AddRange(results);
        }

        // 移除重複的文件區塊 (依據 ID)，並根據原始順序保留最相關的內容
        var distinctResults = allResults
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .Take(5) // 最後只取前 5 個最相關的區塊
            .ToList();

        // 5. 產出最終 Prompt Context
        Console.WriteLine("\n[3/3] 正在生成最終的 Prompt Context...");
        
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("你是一個專業的 AI 助手。請根據下方提供的【參考資料】來回答使用者的問題。");
        promptBuilder.AppendLine("如果資料中沒有相關資訊，請誠實告知，不要胡亂猜測。");
        promptBuilder.AppendLine("\n【參考資料】:");
        
        foreach (var doc in distinctResults)
        {
            promptBuilder.AppendLine($"--- 來源: {doc.Source} ---");
            promptBuilder.AppendLine(doc.Content);
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("【使用者問題】:");
        promptBuilder.AppendLine(userQuery);
        promptBuilder.AppendLine("\n【請開始回答】:");

        var finalPrompt = promptBuilder.ToString();

        Console.WriteLine("\n==================== 最終生成的 Prompt ====================");
        Console.WriteLine(finalPrompt);
        Console.WriteLine("===========================================================");
        
        Console.WriteLine("\n範例執行完成。你可以將上述 Prompt 發送給 LLM 得到最終答案。");
    }

    private static List<RawDoc> GetSampleData()
    {
        return new List<RawDoc>
        {
            new RawDoc 
            { 
                Title = "LanceDB 介紹", 
                Content = "LanceDB 是一個開源的向量資料庫，專為 AI 應用程式設計。它基於 Lance 格式，這是一種高性能的柱狀資料格式。LanceDB 的核心特點包括：無伺服器架構、高性能向量搜尋、以及整合資料管理。它採用磁碟優先設計，即使資料量大於記憶體，也能保持高效能。" 
            },
            new RawDoc
            {
                Title = "RAG 技術概覽",
                Content = "檢索增強生成 (RAG) 結合了 LLM 的生成能力與向量資料庫的檢索能力。透過查詢擴展 (Query Expansion) 技術，可以生成多個變體查詢，從而提高檢索召回率。重疊切塊 (Overlap Chunking) 則確保了區塊間的脈絡不中斷，提供更完整的上下文資訊給模型。"
            }
        };
    }

    private class RawDoc
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
