# MyRAG.Reranking.Onnx

這是 `MyRAG.Core` 的本地 Reranker 實作擴充專案。透過 **ONNX Runtime** 與 **DirectML**，本專案允許您在本地執行 Cross-Encoder 模型，對檢索到的文件進行精確的二次排名。

## 為什麼需要 Reranking？

傳統的向量檢索 (Bi-Encoder) 雖然速度快，但語義捕捉能力有限。Reranking (Cross-Encoder) 會將「查詢」與「文件」同時輸入模型進行深度分析，能顯著提升檢索結果的準確度，過濾掉不相關的雜訊。

## 特點

- **本地 GPU 加速**：支援 DirectML，可充分利用 AMD Radeon 780M 等 iGPU 效能。
- **高相容性**：支援常見的 BGE-Reranker 與 Cross-Encoder 模型。
- **自動處理輸入**：自動根據模型中繼資料處理 `position_ids` 與 `past_key_values`。

## 安裝與配置

### 1. 專案依賴
- `Microsoft.ML.OnnxRuntime.DirectML`
- `Microsoft.ML.Tokenizers`
- `MyRAG.Core`

### 2. 註冊服務
在您的應用程式啟動配置中 (如 `Program.cs`)，註冊 OnnxReranker：

```csharp
using MyRAG.Reranking.Onnx.Extensions;

// 註冊 Reranker
builder.Services.AddOnnxReranker(
    modelPath: "D:\\models\\bge-reranker\\model.onnx", 
    tokenizerJsonPath: "D:\\models\\bge-reranker\\tokenizer.json",
    useGPU: true
);
```

## 使用範例

當 `IReranker` 被註冊後，`RetrievalPipeline` 會自動在向量檢索後執行重排：

```csharp
// 在 RetrievalPipeline 內部會自動呼叫：
var rankedResults = await _reranker.RerankAsync(query, retrievedDocuments);
```

您也可以單獨使用它：

```csharp
public async Task Sample(IReranker reranker, string query, List<Document> docs)
{
    var results = await reranker.RerankAsync(query, docs);
    foreach (var res in results)
    {
        Console.WriteLine($"Score: {res.Score}, Content: {res.Item.Content}");
    }
}
```

## 推薦模型
建議使用以下模型以獲得最佳的重排效果：
- **Qwen3-Reranker-0.6B-ONNX**: [onnx-community/Qwen3-Reranker-0.6B-ONNX](https://huggingface.co/onnx-community/Qwen3-Reranker-0.6B-ONNX)

## 注意事項

1. **模型選擇**：除了上述推薦模型，也可以使用 `bge-reranker-v2-m3` 或 `bge-reranker-base` 的 ONNX 版本。
2. **Tokenizer**：目前實作採用 Tiktoken 作為通用處理，針對特定模型（如 BERT 體系）建議未來擴充專用 Tokenizer。
3. **效能**：Cross-Encoder 的運算開銷大於 Embedding，建議僅對檢索出的前 10-50 筆結果進行 Rerank。

---
*Powered by ONNX Runtime & MyRAG Framework*
