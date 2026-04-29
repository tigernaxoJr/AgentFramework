根據 https://huggingface.co/onnx-community/Qwen3-Embedding-0.6B-ONNX/tree/main/onnx
model_quantized.onnx 已經放在 "D:\onnx\qwen3-embedding-0.6B\model_quantized.onnx"
tokenizer.json 已經放在 "D:\onnx\qwen3-embedding-0.6B\tokenizer.json"
tokenizer_config.json 已經放在 "D:\onnx\qwen3-embedding-0.6B\tokenizer_config.json"
以下是實作建議內容:

在 .NET 環境中透過 ONNX 執行 Embedding 任務，目前最主流且效能最佳的選擇是使用 **Microsoft.ML.OnnxRuntime**。針對你提到的 iGPU（Radeon 780M）需求，我們會利用 **DirectML** 加速器，這是在 Windows 環境下調用非 NVIDIA 顯卡（如 AMD iGPU）的最穩健方案。

以下是完整的設定與實作流程：

---

### 1. 套件安裝 (NuGet)

你需要安裝以下三個核心套件：

- **Microsoft.ML.OnnxRuntime.DirectML**: 用於支援 CPU 與 GPU (DirectML) 加速。
- **Microsoft.ML.Tokenizers**: Microsoft 官方提供的 Tokenizer 套件，用於處理 `tokenizer.json`。
- **System.Numerics.Tensors**: 輔助處理數組運算。

```bash
dotnet add package Microsoft.ML.OnnxRuntime.DirectML
dotnet add package Microsoft.ML.Tokenizers
dotnet add package System.Numerics.Tensors
```

---

### 2. 環境與檔案準備

請確保你的專案目錄中包含以下檔案：

1.  `model.onnx`: 你的 Embedding 模型。
2.  `tokenizer.json`: 模型對應的詞表配置。
3.  `tokenizer_config.json`: 雖然 `Microsoft.ML.Tokenizers` 主要讀取 `tokenizer.json`，但此檔案通常包含 `padding` 或 `truncation` 的邏輯參考。

---

### 3. 範例程式碼

這個範例展示了如何根據硬體狀況切換裝置，並執行 Text-to-Embedding。

```csharp
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.Numerics.Tensors;

public class EmbeddingService
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private readonly int _dim = 1024; // 根據模型調整 (如 Qwen3-Embedding 可能不同)

    public EmbeddingService(string modelPath, string tokenizerJsonPath, bool useGPU = true)
    {
        // 1. 初始化 Tokenizer
        // Microsoft.ML.Tokenizers 直接支援從 tokenizer.json 載入
        var model = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenizerModel>(File.ReadAllText(tokenizerJsonPath));
        _tokenizer = new Tokenizer(new Bpe()); // 注意：需根據模型類型選擇 Bpe 或 WordPiece

        // 2. 設定 ONNX Session
        var options = new SessionOptions();
        if (useGPU)
        {
            try {
                options.AppendExecutionProvider_DML(0); // 使用 DirectML (AMD 780M 適用)
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            } catch {
                // 如果 DML 初始化失敗，自動降級到 CPU
                Console.WriteLine("GPU 加速啟動失敗，切換回 CPU 模式。");
            }
        }

        _session = new InferenceSession(modelPath, options);
    }

    public float[] GetEmbedding(string text)
    {
        // 3. Tokenization
        var encoding = _tokenizer.Encode(text);
        long[] inputIds = encoding.Ids.Select(x => (long)x).ToArray();
        long[] attentionMask = encoding.AttentionMask.Select(x => (long)x).ToArray();
        long[] typeIds = new long[inputIds.Length]; // 通常為 0

        // 建立輸入 Tensor (Shape: [1, sequence_length])
        var shape = new int[] { 1, inputIds.Length };
        var inputIdsTensor = new DenseTensor<long>(inputIds, shape);
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, shape);
        var typeIdsTensor = new DenseTensor<long>(typeIds, shape);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", typeIdsTensor)
        };

        // 4. 執行推理
        using var results = _session.Run(inputs);

        // 假設輸出節點名為 "last_hidden_state" 或 "sentence_embedding"
        // 注意：這裡通常需要做 Mean Pooling，如果 ONNX 沒內建的話
        var outputTensor = results.First().AsTensor<float>();

        // 簡單示範：取第一個 Token ([CLS]) 或執行平均池化
        return Normalize(ExtractMeanPooling(outputTensor, attentionMask));
    }

    private float[] ExtractMeanPooling(Tensor<float> lastHiddenState, long[] mask)
    {
        // 實作 Mean Pooling (加權平均) 以獲得更高品質的向量
        int seqLen = (int)lastHiddenState.Dimensions[1];
        int dim = (int)lastHiddenState.Dimensions[2];
        float[] mean = new float[dim];

        for (int i = 0; i < seqLen; i++)
        {
            if (mask[i] == 0) continue;
            for (int d = 0; d < dim; d++)
                mean[d] += lastHiddenState[0, i, d];
        }

        float count = mask.Sum();
        for (int d = 0; d < dim; d++) mean[d] /= count;
        return mean;
    }

    private float[] Normalize(float[] vector)
    {
        // L2 Normalization (餘弦相似度必備)
        float norm = MathF.Sqrt(vector.Sum(x => x * x));
        return vector.Select(x => x / norm).ToArray();
    }
}
```

---

### 4. 設定細節與注意事項

#### 硬體切換邏輯 (CPU vs iGPU)

- 你的 **Ryzen 9 7940HS** 內建的 **780M** 效能極佳，非常適合跑 Embedding。
- `AppendExecutionProvider_DML(0)`：`0` 通常代表第一張 GPU。在沒有獨立顯卡的筆電上，這就是 780M。
- **記憶體管理**：由於你有 **64GB RAM**，建議在 `SessionOptions` 中開啟 `EnableMemoryPattern = true` 以優化記憶體重用。

#### Tokenizer 處理

- `tokenizer_config.json` 內通常定義了 `max_length`（例如 512 或 8192）。在 C# 端處理字串時，務必手動或透過 Tokenizer 參數進行 `Truncate`，避免輸入長度超過 ONNX 模型定義的靜態/動態維度限制。

#### 關於 Mean Pooling

大部分 HuggingFace 的模型（如 `bge-m3`, `multilingual-e5`）輸出的原始 Tensor 形狀是 `[Batch, Seq, Dim]`。

- 如果你需要與 Python 的 `sentence-transformers` 結果一致，**必須**實作 Mean Pooling。
- 如果你的 ONNX 模型在匯出時已經包含了 Pooling Layer（輸出直接是 `[Batch, Dim]`），則直接取值即可。

### 5. 快速測試

```csharp
var service = new EmbeddingService("model.onnx", "tokenizer.json", useGPU: true);
var vector = service.GetEmbedding("這是一個測試");
Console.WriteLine($"維度: {vector.Length}, 前三碼: {vector[0]}, {vector[1]}, {vector[2]}");
```

這套流程能完美適配你目前的醫療法規 RAG 系統開發，特別是在醫院內部環境中，利用 780M iGPU 可以在不依賴雲端 API 的情況下，獲得極高的向量化速度。
