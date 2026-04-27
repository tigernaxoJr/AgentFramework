using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.Samples.Infrastructure;
using MyRAG.Samples.Samples;

namespace MyRAG.Samples.Samples;

/// <summary>
/// 範例 05：RagEngine 端對端流程
/// 展示如何使用 IRagEngine 以一致的管線完成：
///   1. 資料匯入（Ingestion）：自動切塊 → 生成 Embedding → 存入 LanceDB
///   2. 資料檢索（Retrieval）：查詢 → 向量搜尋 → 排名融合
/// ⚠️ 需要 Embedding API 連線。
/// </summary>
public class RagEngineEndToEndExample(IRagEngine ragEngine) : SampleBase
{
    private readonly IRagEngine _ragEngine = ragEngine;

    public async Task RunAsync()
    {
        PrintHeader("範例 05：RagEngine 端對端流程 (End-to-End)");

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  ⚠ 此範例需要 Embedding API 連線。");
        Console.ResetColor();
        Console.WriteLine();

        // ── 步驟 1：匯入資料 ──────────────────────────────────────────
        PrintStep("步驟 1 / 2：執行資料匯入管線 (Ingestion Pipeline)");
        Console.WriteLine();

        // 使用原始長文本，讓管線自動切塊
        var rawDocuments = new List<Document>
        {
            new()
            {
                Id = "e2e-001",
                Content = """
                    大型語言模型（LLM）是以 Transformer 架構為基礎的深度學習模型，
                    經過海量文本訓練後具備了強大的語言理解與生成能力。
                    GPT-4、Claude、Gemini 是目前最知名的 LLM 代表。
                    這些模型雖然功能強大，但有時會產生「幻覺」——也就是生成看似合理但實際上不正確的資訊。
                    RAG 技術正是為了解決這個問題而生：透過檢索外部知識庫，為 LLM 提供準確的上下文。
                    """,
                Source = "llm_rag_intro.txt"
            },
            new()
            {
                Id = "e2e-002",
                Content = """
                    MyRAG.Core 框架採用介面優先的設計哲學，所有核心功能皆以介面定義：
                    ITextChunkingService 負責文本切塊，IEmbeddingService 負責向量生成，
                    IVectorStore 抽象了底層的向量資料庫操作，IRankFusion 實作排名融合演算法。
                    開發者可以自由替換任何一個元件，而不影響整體管線的運作。
                    例如，可以將 LanceDB 替換為 PostgreSQL（搭配 pgvector），
                    或是將 OpenAI Embedding 替換為本地端的 Ollama 模型。
                    """,
                Source = "myrag_design.txt"
            }
        };

        PrintInfo("待匯入文件數", rawDocuments.Count.ToString());
        PrintInfo("切塊策略", "Batched (預設，帶重疊)");

        try
        {
            var swIngest = System.Diagnostics.Stopwatch.StartNew();
            await _ragEngine.Ingestion.IngestAsync(rawDocuments, ChunkingStrategy.Batched);
            swIngest.Stop();

            PrintSuccess($"資料匯入完成！耗時：{swIngest.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n  ✘ 匯入失敗：{ex.Message}");
            Console.ResetColor();
            WaitForKey("按任意鍵結束此範例...");
            return;
        }

        // ── 步驟 2：查詢資料 ──────────────────────────────────────────
        Console.WriteLine();
        PrintStep("步驟 2 / 2：執行資料檢索管線 (Retrieval Pipeline)");
        Console.WriteLine();

        string[] queries = ["RAG 如何解決幻覺問題？", "MyRAG 框架如何切換向量資料庫？"];

        foreach (var query in queries)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  🔍 查詢：");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\"{query}\"");
            Console.ResetColor();

            try
            {
                var swRetrieve = System.Diagnostics.Stopwatch.StartNew();
                var results = await _ragEngine.Retrieval.RetrieveAsync(query, topK: 3);
                swRetrieve.Stop();

                var resultList = results.ToList();
                PrintInfo("耗時", $"{swRetrieve.ElapsedMilliseconds} ms，取回 {resultList.Count} 筆");

                for (int i = 0; i < resultList.Count; i++)
                {
                    PrintResult(i + 1, resultList[i].Item.Content, resultList[i].Score);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✘ 查詢失敗：{ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        PrintSuccess("端對端範例完成！");
        WaitForKey();
    }
}
