using MyRAG.Core.Interfaces;
using MyRAG.Samples.Infrastructure;

namespace MyRAG.Samples.Samples;

/// <summary>
/// 範例 02：語義切塊 (Semantic Chunking)
/// 展示如何利用 Embedding 分析句意相似度，在語意轉折處自動切分段落。
/// ⚠️ 需要 Embedding API 才能執行（LM Studio / Ollama / OpenAI）。
/// </summary>
public class SemanticChunkingExample(ITextChunkingService chunkingService) : SampleBase
{
    private readonly ITextChunkingService _chunkingService = chunkingService;

    public async Task RunAsync()
    {
        PrintHeader("範例 02：語義切塊 (Semantic Chunking)");

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  ⚠ 此範例需要 Embedding API 連線。請確認 appsettings.local.json 設定正確。");
        Console.ResetColor();
        Console.WriteLine();

        // 刻意設計成兩個主題交替的文本，測試語義切塊能否正確分段
        var mixedTopicText = """
            機器學習是人工智慧的一個子領域，透過讓電腦從數據中自動學習規律。
            深度學習是機器學習的進階技術，使用多層神經網路來處理複雜問題。
            卷積神經網路（CNN）特別擅長處理圖像辨識任務。
            自然語言處理（NLP）則讓電腦能夠理解和生成人類語言。

            台灣的夜市文化非常獨特，各地都有具代表性的夜市。
            士林夜市是台北最大的夜市，提供各式各樣的台灣小吃。
            基隆廟口夜市以海鮮料理聞名，是許多饕客必訪之地。
            逢甲夜市在台中，以創意小吃和年輕化的飲食風格吸引人潮。

            大型語言模型（LLM）是近年 AI 最重要的突破之一。
            GPT 系列模型由 OpenAI 開發，具備強大的文字生成能力。
            RAG（Retrieval-Augmented Generation）技術結合了檢索與生成，提升了 LLM 的準確性與時效性。
            """;

        PrintStep("原始文本包含混合主題（AI技術 + 台灣夜市 + LLM）");
        PrintStep("執行語義切塊 (CreateSemanticChunksAsync)...");

        try
        {
            var semanticChunks = await _chunkingService.CreateSemanticChunksAsync(mixedTopicText);
            var chunkList = semanticChunks.ToList();

            PrintSuccess($"語義切塊完成，共 {chunkList.Count} 個 chunk");
            Console.WriteLine();

            for (int i = 0; i < chunkList.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($"\n  ── Chunk {i + 1} ──");
                Console.ResetColor();
                Console.WriteLine();
                PrintResult(i + 1, chunkList[i]);
            }

            // 比較：同一文本用 Batched 切塊
            Console.WriteLine();
            PrintStep("對照：同一文本用 Batched 切塊結果...");
            var batchedChunks = _chunkingService.CreateBatchedChunks(mixedTopicText).ToList();
            PrintSuccess($"Batched 切塊共 {batchedChunks.Count} 個 chunk（語義切塊：{chunkList.Count} 個）");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n  ✘ 語義切塊失敗（請確認 Embedding API 是否正常運作）");
            Console.WriteLine($"  錯誤：{ex.Message}");
            Console.ResetColor();
        }

        WaitForKey();
    }
}
