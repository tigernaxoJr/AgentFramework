using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.Samples.Infrastructure;

namespace MyRAG.Samples.Samples;

/// <summary>
/// 範例 03：LanceDB 向量資料庫 - 資料匯入 (Ingestion)
/// 展示如何將一批文件透過 IVectorStore (LanceDB 實作) 進行向量化後儲存。
/// ⚠️ 需要 Embedding API 連線。
/// </summary>
public class LanceDBIngestionExample(IVectorStore vectorStore) : SampleBase
{
    private readonly IVectorStore _vectorStore = vectorStore;

    /// <summary>取得範例文件集合（模擬知識庫內容）</summary>
    public static List<Document> GetSampleDocuments() =>
    [
        new Document
        {
            Id = "doc-001",
            Content = "LanceDB 是一個開源的嵌入式向量資料庫，使用 Apache Arrow 格式存儲數據，支援高效的向量相似度搜尋。它以 Rust 編寫，提供 Python 和 .NET 的 SDK。",
            Source = "lancedb_intro.md",
            Metadata = new() { { "category", "database" }, { "lang", "zh-TW" } }
        },
        new Document
        {
            Id = "doc-002",
            Content = "RAG（Retrieval-Augmented Generation）是一種結合資訊檢索與大型語言模型的技術。它先從知識庫中取出相關文件，再將其作為上下文交給 LLM 生成回答，有效解決幻覺問題。",
            Source = "rag_overview.md",
            Metadata = new() { { "category", "ai" }, { "lang", "zh-TW" } }
        },
        new Document
        {
            Id = "doc-003",
            Content = "向量嵌入（Vector Embedding）是將文本、圖片等非結構化資料轉換為高維數值向量的過程。語義相近的內容在向量空間中距離較近，這是語義搜尋的核心原理。",
            Source = "embedding_intro.md",
            Metadata = new() { { "category", "ai" }, { "lang", "zh-TW" } }
        },
        new Document
        {
            Id = "doc-004",
            Content = "台積電（TSMC）是全球最大的晶圓代工廠，成立於 1987 年。台積電的先進製程技術（如 3nm、2nm）引領全球半導體產業，蘋果、NVIDIA、AMD 等大廠都是其主要客戶。",
            Source = "tsmc_info.md",
            Metadata = new() { { "category", "business" }, { "lang", "zh-TW" } }
        },
        new Document
        {
            Id = "doc-005",
            Content = ".NET 是微軟開發的跨平台開源框架，支援 Windows、Linux 和 macOS。最新版本為 .NET 10，提供了高效能的執行環境和豐富的類別庫，廣泛用於企業應用開發。",
            Source = "dotnet_intro.md",
            Metadata = new() { { "category", "technology" }, { "lang", "zh-TW" } }
        },
        new Document
        {
            Id = "doc-006",
            Content = "MyRAG.Core 是一個為 .NET 平台設計的 RAG 框架，提供文本切塊、向量化、排名融合等核心功能。它採用介面優先設計，支援多種向量資料庫後端，包括 LanceDB、PostgreSQL 等。",
            Source = "myrag_overview.md",
            Metadata = new() { { "category", "framework" }, { "lang", "zh-TW" } }
        }
    ];

    public async Task RunAsync()
    {
        PrintHeader("範例 03：LanceDB 向量資料庫 - 資料匯入 (Ingestion)");

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  ⚠ 此範例需要 Embedding API 連線，並會在本地建立 LanceDB 資料庫。");
        Console.ResetColor();
        Console.WriteLine();

        var documents = GetSampleDocuments();

        PrintStep($"準備匯入 {documents.Count} 份示範文件...");
        foreach (var doc in documents)
        {
            PrintInfo($"  - [{doc.Id}]", doc.Content[..Math.Min(50, doc.Content.Length)] + "...");
        }

        Console.WriteLine();
        PrintStep("呼叫 IVectorStore.UpsertAsync()...");
        PrintInfo("後端實作", "LanceDBVectorStore");
        PrintInfo("資料庫路徑", "./lancedb_data");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _vectorStore.UpsertAsync(documents);
            sw.Stop();

            PrintSuccess($"成功匯入 {documents.Count} 份文件！耗時：{sw.ElapsedMilliseconds} ms");
            Console.WriteLine();
            PrintInfo("提示", "資料已持久化至磁碟，下次啟動程式仍可查詢。");
            PrintInfo("提示", "執行「範例 04」查看向量搜尋效果。");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n  ✘ 匯入失敗：{ex.Message}");
            Console.ResetColor();
        }

        WaitForKey();
    }
}
