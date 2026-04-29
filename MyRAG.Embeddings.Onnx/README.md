# MyRAG.Embeddings.Onnx

此專案提供了基於 **ONNX Runtime** 與 **DirectML** 的本地向量嵌入 (Embedding) 實作，特別針對 Windows 環境下的 **AMD iGPU (如 Radeon 780M)** 進行了優化。

## 特點

- **本地執行**: 無需聯網或呼叫外部 API (如 OpenAI)，保護數據隱私。
* **硬體加速**: 透過 `Microsoft.ML.OnnxRuntime.DirectML` 支援 GPU 加速，大幅提升效能。
- **高效能**: 使用 `System.Numerics.Tensors` 進行向量處理與 L2 正規化。
- **標準介面**: 實作 `IEmbeddingGenerator<string, Embedding<float>>`，可無縫集成至 `Microsoft.Extensions.AI` 生態系。

## 安裝

在您的專案中引用此專案，或安裝相關 NuGet 套件：

```bash
dotnet add package Microsoft.ML.OnnxRuntime.DirectML
dotnet add package Microsoft.ML.Tokenizers
dotnet add package System.Numerics.Tensors
```

## 使用方法

### 1. 註冊服務 (Dependency Injection)

```csharp
using MyRAG.Embeddings.Onnx.Extensions;

services.AddOnnxEmbeddingGenerator(
    modelPath: @"D:\onnx\model.onnx",
    tokenizerPath: @"D:\onnx\tokenizer.json",
    useGPU: true
);
```

### 2. 生成向量

```csharp
public class MyService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public MyService(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    public async Task ProcessAsync()
    {
        var embeddings = await _generator.GenerateAsync(new[] { "測試文本" });
        var vector = embeddings[0].Vector;
        // ...
    }
}
```

## 注意事項

- **Tokenizer**: 目前預設使用 `TiktokenTokenizer` (如 GPT-4 格式)。若您的模型 (如 Qwen) 使用特殊 Tokenizer，請確保 `tokenizer.json` 格式相容。
- **Mean Pooling**: 針對輸出的 `last_hidden_state` (形狀為 `[Batch, Seq, Dim]`)，此實作會自動執行 Mean Pooling 以獲得句向量。
- **L2 Normalization**: 輸出向量會自動進行 L2 正規化，適合直接進行餘弦相似度計算。
