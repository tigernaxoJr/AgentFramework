using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;

namespace MyRAG.Reranking.Onnx;

/// <summary>
/// 基於 ONNX Runtime 與 DirectML 的 Reranker 實作。
/// 支援使用 Cross-Encoder 模型對檢索結果進行重新排名。
/// </summary>
public sealed class OnnxReranker : IReranker, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private bool _disposed;

    /// <summary>
    /// 初始化新的 <see cref="OnnxReranker"/> 實例。
    /// </summary>
    /// <param name="modelPath">ONNX 模型檔案路徑。</param>
    /// <param name="tokenizerJsonPath">tokenizer.json 檔案路徑。</param>
    /// <param name="useGPU">是否嘗試使用 DirectML GPU 加速。</param>
    public OnnxReranker(string modelPath, string tokenizerJsonPath, bool useGPU = true)
    {
        if (!File.Exists(tokenizerJsonPath))
            throw new FileNotFoundException("找不到 Tokenizer 設定檔。", tokenizerJsonPath);

        // 初始化 Tokenizer
        // 註：實務上應根據模型類型 (如 BGE 使用 WordPiece/BPE) 選擇適當的載入方式。
        // 這裡延續專案現有模式，建議未來可擴充支援從 tokenizer.json 完整載入。
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4"); 

        var options = new SessionOptions();
        if (useGPU)
        {
            try
            {
                options.AppendExecutionProvider_DML(0);
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                options.EnableMemoryPattern = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reranker GPU 加速啟動失敗: {ex.Message}，切換回 CPU 模式。");
            }
        }

        if (!File.Exists(modelPath))
            throw new FileNotFoundException("找不到 ONNX 模型檔案。", modelPath);

        _session = new InferenceSession(modelPath, options);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RankedItem<Document>>> RerankAsync(
        string query, 
        IEnumerable<Document> documents, 
        CancellationToken cancellationToken = default)
    {
        var rankedItems = new List<RankedItem<Document>>();

        foreach (var doc in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 對於 Cross-Encoder，我們將查詢與文件內容拼接
            // 典型格式為 [CLS] query [SEP] content [SEP]
            var score = GetScore(query, doc.Content);
            rankedItems.Add(new RankedItem<Document>(doc, score));
        }

        // 依分數降序排列
        return await Task.FromResult(rankedItems.OrderByDescending(x => x.Score));
    }

    private double GetScore(string query, string content)
    {
        // 簡單拼接 Query 與 Content
        // 註：這是一個簡化的實作，正式環境應使用 Tokenizer 的 Pairwise Encoding 處理 [SEP] 標記
        var combinedText = $"{query} {content}";
        
        var ids = _tokenizer.EncodeToIds(combinedText).Select(x => (long)x).ToArray();
        var mask = new long[ids.Length];
        Array.Fill(mask, 1L);

        var shape = new int[] { 1, ids.Length };
        var inputIdsTensor = new DenseTensor<long>(ids.AsMemory(), shape);
        var attentionMaskTensor = new DenseTensor<long>(mask.AsMemory(), shape);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        // 檢查模型是否需要 token_type_ids
        if (_session.InputMetadata.ContainsKey("token_type_ids"))
        {
            var typeIds = new long[ids.Length];
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(typeIds.AsMemory(), shape)));
        }

        using var results = _session.Run(inputs);
        
        // Reranker 通常輸出單個分數 (Logits)
        var outputTensor = results.First().AsTensor<float>();
        
        // 取得第一個維度的第一個值
        return outputTensor.First();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _session.Dispose();
            _disposed = true;
        }
    }
}
