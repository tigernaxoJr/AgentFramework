using MyRAG.Core.Interfaces;
using MyRAG.Samples.Infrastructure;

namespace MyRAG.Samples.Samples;

/// <summary>
/// 範例 04：LanceDB 向量資料庫 - 語義搜尋 (Retrieval)
/// 展示如何對已存入 LanceDB 的文件進行向量相似度查詢。
/// ⚠️ 請先執行範例 03 匯入資料，且需要 Embedding API 連線。
/// </summary>
public class LanceDBRetrievalExample(IVectorStore vectorStore) : SampleBase
{
    private readonly IVectorStore _vectorStore = vectorStore;

    private static readonly string[] SampleQueries =
    [
        "什麼是向量資料庫？",
        "如何解決大型語言模型的幻覺問題？",
        "台灣半導體產業",
        ".NET 跨平台開發",
        "RAG 框架有哪些功能？"
    ];

    public async Task RunAsync()
    {
        PrintHeader("範例 04：LanceDB 向量資料庫 - 語義搜尋 (Retrieval)");

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  ⚠ 請先執行範例 03 匯入資料，再執行此範例。");
        Console.ResetColor();
        Console.WriteLine();

        const int topK = 3;

        foreach (var query in SampleQueries)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  🔍 查詢：");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\"{query}\"");
            Console.ResetColor();

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var results = (await _vectorStore.SearchAsync(query, topK)).ToList();
                sw.Stop();

                PrintInfo("耗時", $"{sw.ElapsedMilliseconds} ms，取回 {results.Count} 筆結果");

                if (results.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  （無結果，請先執行範例 03 匯入資料）");
                    Console.ResetColor();
                    continue;
                }

                for (int i = 0; i < results.Count; i++)
                {
                    var doc = results[i];
                    PrintResult(i + 1, doc.Content, score: null); // LanceDB 目前不直接回傳 score
                    if (doc.Source != null)
                        PrintInfo("    來源", doc.Source);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✘ 查詢失敗：{ex.Message}");
                Console.ResetColor();
            }
        }

        // 互動式查詢
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ── 互動式查詢模式（輸入 'exit' 離開）──");
        Console.ResetColor();

        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  請輸入查詢語句：");
            Console.ResetColor();

            var userInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            try
            {
                var results = (await _vectorStore.SearchAsync(userInput, topK)).ToList();
                PrintSuccess($"取回 {results.Count} 筆結果");

                for (int i = 0; i < results.Count; i++)
                {
                    PrintResult(i + 1, results[i].Content);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✘ 查詢失敗：{ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
