using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using System.Diagnostics;

namespace MyRAG.Samples.Samples;

public class OnnxRerankingExample
{
    private readonly IReranker? _reranker;

    public OnnxRerankingExample(IReranker? reranker = null)
    {
        _reranker = reranker;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        範例 08: ONNX 本地 Reranking (DirectML)          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (_reranker == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[!] 未偵測到已註冊的 IReranker。請確保在 appsettings.json 中啟動 OnnxReranker。");
            Console.ResetColor();
            Console.WriteLine("\n按任意鍵返回選單...");
            Console.ReadKey(intercept: true);
            return;
        }

        var query = "如何處理醫療糾紛的爭議調解？";
        
        // 模擬檢索到的文件 (混合了相關與不相關的內容)
        var documents = new List<Document>
        {
            new Document { Content = "醫療法規定，發生醫療爭議時，應先經直轄市、縣（市）主管機關調解。這是一個有關爭議處理的流程說明。", Metadata = new Dictionary<string, object> { ["source"] = "醫療法第 82 條" } },
            new Document { Content = "醫院應設有病歷室，專人管理病歷，並應依規定年限保存。病歷之保存與調解無直接關係。", Metadata = new Dictionary<string, object> { ["source"] = "醫療法第 70 條" } },
            new Document { Content = "病人就醫時，應向醫療機構說明其健康狀況、過去病史、過敏史等資訊。", Metadata = new Dictionary<string, object> { ["source"] = "醫療法第 48 條" } },
            new Document { Content = "直轄市、縣（市）主管機關應設醫療爭議調解委員會，處理醫療爭議之調解事項。這是調解的組織依據。", Metadata = new Dictionary<string, object> { ["source"] = "醫療法第 99 條" } },
            new Document { Content = "醫師執行業務時，應製作病歷，記載病人姓名、出生年月日、性別、住址等基本資料。", Metadata = new Dictionary<string, object> { ["source"] = "醫師法第 12 條" } }
        };

        Console.WriteLine($"[1] 查詢語句: \"{query}\"");
        Console.WriteLine($"[2] 待排序文件數量: {documents.Count}");
        Console.WriteLine();

        Console.WriteLine("    正在執行 Reranking (Cross-Encoder)...");
        var sw = Stopwatch.StartNew();

        try
        {
            var rankedResults = await _reranker.RerankAsync(query, documents);
            sw.Stop();

            Console.WriteLine($"[✔] 排序完成！耗時: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine();

            Console.WriteLine("--- 排序結果 (依相關性分數降序) ---");
            int rank = 1;
            foreach (var result in rankedResults)
            {
                var scoreColor = result.Score > 0 ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  [Rank {rank++}]");
                Console.ForegroundColor = scoreColor;
                Console.Write($" 分數: {result.Score:F4}");
                Console.ResetColor();
                Console.WriteLine($" | 來源: {result.Item.Metadata["source"]}");
                
                var snippet = result.Item.Content.Length > 60 ? result.Item.Content.Substring(0, 60) + "..." : result.Item.Content;
                Console.WriteLine($"    內容: {snippet}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✘] 發生錯誤: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n按任意鍵返回選單...");
        Console.ReadKey(intercept: true);
    }
}
