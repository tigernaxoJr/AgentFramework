using MyRAG.Core.Interfaces;
using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace MyRAG.Samples.Samples;

// 建議模型: https://huggingface.co/onnx-community/Qwen3-Embedding-0.6B-ONNX
public class OnnxEmbeddingExample
{
    private readonly IEmbeddingService _embeddingService;

    public OnnxEmbeddingExample(IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        範例 07: ONNX 本地 Embedding (DirectML)          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var texts = new List<string>
        {
            "這是一個使用 ONNX Runtime 執行的本地向量嵌入測試。",
            "MyRAG 框架支援多種 Embedding 生成器，包括 OpenAI 與 ONNX。",
            "在 AMD Radeon 780M iGPU 上執行 Embedding 可以顯著提升效能且不需聯網。",
            "醫療法規 RAG 系統通常需要在院內環境離線運行，ONNX 是絕佳選擇。"
        };

        Console.WriteLine($"[1] 準備生成 {texts.Count} 段文本的向量...");
        Console.WriteLine("    使用模型: Qwen3-Embedding-0.6B-ONNX (DirectML 加速)");
        Console.WriteLine();

        var sw = Stopwatch.StartNew();
        
        try
        {
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);
            sw.Stop();

            Console.WriteLine($"[✔] 生成完成！耗時: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine();

            for (int i = 0; i < texts.Count; i++)
            {
                var vector = embeddings[i].Vector;
                Console.WriteLine($"  文本 {i + 1}: {texts[i].Substring(0, Math.Min(texts[i].Length, 30))}...");
                Console.WriteLine($"  向量維度: {vector.Length}");
                Console.WriteLine($"  前 5 碼: [{string.Join(", ", vector.Span.ToArray().Take(5).Select(v => v.ToString("F4")))}...]");
                Console.WriteLine();
            }

            // 餘弦相似度示範
            Console.WriteLine("[2] 餘弦相似度簡易測試:");
            var sim = CalculateCosineSimilarity(embeddings[0].Vector.ToArray(), embeddings[1].Vector.ToArray());
            Console.WriteLine($"    相似度 (文本 1 vs 2): {sim:F4}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[✘] 發生錯誤: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"    內部錯誤: {ex.InnerException.Message}");
            Console.ResetColor();
            Console.WriteLine("\n請確保已下載模型並正確設定路徑。");
            Console.WriteLine("推薦模型下載: https://huggingface.co/onnx-community/Qwen3-Embedding-0.6B-ONNX");
        }

        Console.WriteLine("\n按任意鍵返回選單...");
        Console.ReadKey(intercept: true);
    }

    private float CalculateCosineSimilarity(float[] vec1, float[] vec2)
    {
        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < vec1.Length; i++)
        {
            dot += vec1[i] * vec2[i];
            mag1 += vec1[i] * vec1[i];
            mag2 += vec2[i] * vec2[i];
        }
        return dot / (MathF.Sqrt(mag1) * MathF.Sqrt(mag2));
    }
}
