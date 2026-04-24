# MyRAG.Core

`MyRAG.Core` 是一個專為 .NET 平台設計的 RAG (Retrieval-Augmented Generation) 系統核心框架。它提供了一套標準化的介面與基礎實作，涵蓋了從文本前處理、向量化分析到檢索結果融合的核心流程。

## 🚀 主要功能

### 1. 進階文本切塊 (Advanced Chunking)
*   **重疊切塊 (Overlap Chunking)**：在段落切分時自動保留指定長度 (Tokens) 的重複內容，確保關鍵資訊不因切分而遺失脈絡。
*   **語義切塊 (Semantic Chunking)**：具備智慧感知能力，利用 Embedding 分析句意轉折點 (Threshold)，比起單純的長度切分能更精準地完整保留語意單元。

### 2. 向量化服務 (Embeddings)
*   整合 `Microsoft.Extensions.AI`，提供一致的 Embedding 產生介面。
*   支援批次 (Batched) 向量生成，提升大規模資料處理的效率。

### 3. 多重排名融合 (Rank Fusion)
*   內建 **RRF (Reciprocal Rank Fusion)** 演算法實作。
*   能夠將來自不同來源（如向量搜尋與關鍵字搜尋）的排序結果進行科學化的權重融合。

### 4. 現代化架構設計
*   **介面優先 (Interface-First)**：所有核心功能皆定義在 `Interfaces` 下，方便模組抽換與 Mock 測試。
*   **標準化模型**：統一的 `Document` 與 `RankedItem` 實體，簡化 Pipeline 的建立。
*   **DI 友善**：內建依賴注入擴充方法，一鍵完成核心服務註冊。

---

## 📂 專案結構

```text
MyRAG.Core/
├── Models/                 # 核心實體 (Document, RankedItem, Options)
├── Interfaces/             # 核心介面 (ITextChunker, IEmbeddingService, etc.)
├── Chunking/               # 文本切塊實作 (Semantic Kernel Based)
├── Embeddings/             # 向量生成實作
├── Ranking/                # 排名演算法實作 (RRF)
├── Extensions/             # 通用擴充工具
└── DependencyInjection/    # DI 服務註冊工具
```

---

## 🏁 快速入門

### 1. 安裝與註冊服務

在您的 `Program.cs` 中使用 `AddMyRagCore` 擴充方法：

```csharp
using MyRAG.Core.DependencyInjection;

// 註冊 RAG 核心服務
builder.Services.AddMyRagCore(options => {
    options.MaxTokensPerParagraph = 256;
    options.OverlapTokens = 50;
    options.SemanticSimilarityThreshold = 0.8;
});
```

### 2. 注入 Embedding Generator (選用)

若要使用「語義切塊」功能，必須注入符合 `IEmbeddingGenerator` 介面的服務。本框架支援 `Microsoft.Extensions.AI` 標準：

**使用 OpenAI / Ollama (OpenAI SDK):**
```csharp
using OpenAI;
using Microsoft.Extensions.AI;

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => 
{
    var client = new OpenAIClient(new ApiKeyCredential("KEY"), new OpenAIClientOptions { 
        Endpoint = new Uri("http://localhost:11434/v1/") 
    });
    return client.GetEmbeddingClient("model-name").AsIEmbeddingGenerator();
});
```

**使用 Azure OpenAI:**
```csharp
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => 
{
    var client = new AzureOpenAIClient(new Uri("ENDPOINT"), new ApiKeyCredential("KEY"));
    return client.GetEmbeddingClient("deployment").AsIEmbeddingGenerator();
});
```

### 3. 使用文本切塊

```csharp
public class MyService(ITextChunkingService chunkingService)
{
    public async Task ProcessText(string content)
    {
        // 方式 A：標準帶重疊的切塊 (不需要 Embedding)
        var batches = chunkingService.CreateBatchedChunks(content);
        
        // 方式 B：語義感知切塊 (需要注入 Embedding Generator)
        var semanticChunks = await chunkingService.CreateSemanticChunksAsync(content);
    }
}
```

### 4. 查詢轉換與優化 (Query Transformation)

解決使用者問題過於簡短或關鍵字不精準的問題：

**註冊服務 (需先註冊 IChatClient):**
```csharp
// 選擇其中一種轉換策略
builder.Services.AddQueryRewriter(); // 查詢重寫/擴充
// 或
builder.Services.AddHyDETransformer(); // 產生假設性文件 (HyDE)
```

**使用方式:**
```csharp
public async Task Search(string userQuery)
{
    // 注入 IQueryTransformer
    string optimizedQuery = await queryTransformer.TransformAsync(userQuery);
    
    // 使用優化後的 Query 進行檢索...
}
```

### 5. 向量資料庫與存儲 (Vector Store)

提供資料的持久化儲存與相似度檢索功能：

**註冊服務:**
```csharp
// 註冊記憶體內部的向量資料庫 (適合測試與小型應用)
builder.Services.AddInMemoryVectorStore();
```

**使用方式:**
```csharp
public async Task ManageDocs(IVectorStore vectorStore, List<Document> docs)
{
    // 匯入文件 (若文件無 Embedding，Store 會自動呼叫 IEmbeddingService 產生)
    await vectorStore.UpsertAsync(docs);
    
    // 相似度搜尋
    var results = await vectorStore.SearchAsync("如何實作 RAG?", topK: 3);
}
```

### 6. 使用 RRF 排名融合

```csharp
public class SearchService(IRankFusion rankFusion)
{
    public List<string> CombineResults(List<string> vecResults, List<string> kwResults)
    {
        var fused = rankFusion.Fuse(new[] { vecResults, kwResults }, take: 10);
        return fused.Select(x => x.Item).ToList();
    }
}
```

---

## 🛠️ 開發與擴充

本專案採用高度抽象化設計，若您需要自定義新的切塊邏輯或排序方式：

1.  在 `Interfaces` 中找到對應的介面。
2.  實作您的具體類別 (例如 `MyCustomReranker : IReranker`)。
3.  在 DI 容器中替換原本的註冊。

## 📜 授權

此專案僅供開發測試與內部框架使用。
