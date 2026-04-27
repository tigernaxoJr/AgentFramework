using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.Samples.Infrastructure;

namespace MyRAG.Samples.Samples;

/// <summary>
/// 範例 01：基礎重疊切塊 (Batched / Overlap Chunking)
/// 展示如何使用 ITextChunkingService 將長文本切成帶重疊的小段落。
/// 此範例不需要 Embedding API，可直接離線執行。
/// </summary>
public class BasicChunkingExample(ITextChunkingService chunkingService) : SampleBase
{
    private readonly ITextChunkingService _chunkingService = chunkingService;

    public async Task RunAsync()
    {
        PrintHeader("範例 01：基礎重疊切塊 (Batched Chunking)");

        var longText = """
            台灣是一個位於東亞的島嶼，面積約 36,000 平方公里，人口約 2,300 萬人。
            台灣的首都是台北市，是政治、經濟與文化的中心。
            台灣的地形多樣，中央山脈縱貫南北，最高峰玉山海拔 3,952 公尺，是東北亞最高峰。
            台灣的氣候屬於亞熱帶氣候，夏季炎熱多雨，冬季溫和，南北氣候略有差異。
            台灣的經濟以科技製造業為主，半導體產業全球聞名，台積電（TSMC）是全球最大的晶圓代工廠。
            台灣的飲食文化豐富多元，夜市文化享譽全球，小籠包、牛肉麵、珍珠奶茶都是知名美食。
            台灣的交通建設完善，高鐵（台灣高速鐵路）連接台北至高雄，最快僅需 90 分鐘。
            台灣擁有許多自然景觀，太魯閣國家公園、阿里山、日月潭都是熱門旅遊景點。
            台灣的教育水準高，擁有多所頂尖大學，如台灣大學、成功大學、清華大學等。
            台灣的民主制度健全，自 1996 年起實施總統直選，是亞洲重要的民主典範之一。
            """;

        PrintStep("原始文本長度統計");
        PrintInfo("字元數", longText.Length.ToString());
        PrintInfo("行數", longText.Split('\n').Length.ToString());

        PrintStep("執行批次重疊切塊 (CreateBatchedChunks)...");
        // CreateBatchedChunks 為同步方法，不需要 Embedding
        // 回傳值為 List<List<string>>（批次 → chunk 列表），展平後顯示
        var batches = _chunkingService.CreateBatchedChunks(longText);
        var allChunks = batches.SelectMany(b => b).ToList();

        PrintInfo("批次數量", batches.Count.ToString());
        PrintSuccess($"共切出 {allChunks.Count} 個 chunk");
        Console.WriteLine();

        for (int i = 0; i < allChunks.Count; i++)
        {
            PrintResult(i + 1, allChunks[i]);
        }

        // 示範建立 Document 物件
        PrintStep("\n將 chunk 轉為 Document 物件...");
        var documents = allChunks.Select((chunk, idx) => new Document
        {
            Content = chunk,
            Source = "taiwan_intro.txt",
            Metadata = new Dictionary<string, object>
            {
                { "chunk_index", idx },
                { "chunk_total", allChunks.Count },
                { "created_at", DateTime.UtcNow.ToString("O") }
            }
        }).ToList();

        PrintSuccess($"成功建立 {documents.Count} 個 Document 物件");
        PrintInfo("範例文件 ID", documents.First().Id);
        PrintInfo("範例 Metadata", $"chunk_index=0, chunk_total={documents.Count}");

        await Task.CompletedTask; // 保持介面一致
        WaitForKey();
    }
}
