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

### 4. RAG 處理管線與引擎 (Pipelines & Engine)
*   **Ingestion Pipeline**：整合文件讀取、自動切塊與向量資料庫儲存的資料匯入流程。
*   **Retrieval Pipeline**：整合查詢轉換、向量檢索、多路融合與重新排序的資料檢索流程。
*   **RagEngine**：集中管理上述雙管線的單一入口點。

### 5. 現代化架構設計
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
├── Pipelines/              # 管線與引擎實作 (Ingestion, Retrieval, RagEngine)
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

### 2. 注入 Embedding 服務

若要使用「語義切塊」或「向量資料庫」功能，必須注入符合 `IEmbeddingGenerator` 介面的服務。本框架內建了對於 OpenAI 相容 API 的快捷設定擴充：

**使用內建的 OpenAI 相容 API 設定 (支援 LM Studio, Ollama, OpenAI 等):**
```csharp
using MyRAG.Core.DependencyInjection;

// 加入相容於 OpenAI 的 Embedding Generator
builder.Services.AddOpenAICompatibleEmbeddingGenerator(
    endpoint: "http://localhost:1234/v1", // 填寫你的相容 API 端點
    apiKey: "lm-studio",                  // 填寫 API Key (本地端通常可填任意值)
    modelId: "text-embedding-nomic-embed-text-v1.5"
);
```

或者，您也可以自行使用 `Microsoft.Extensions.AI` 標準手動註冊 `IEmbeddingGenerator`（例如使用 Azure OpenAI）。

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

`MyRAG.Core` 將底層儲存抽象為 `IVectorStore` 介面，讓您可以自由切換不同的資料庫技術棧。

**預設的測試用服務:**
```csharp
// 註冊記憶體內部的向量資料庫 (適合開發測試與小型應用)
builder.Services.AddInMemoryVectorStore();
```

#### 🛠️ 根據不同技術棧實作關聯式資料庫 (PostgreSQL, SQL Server, SQLite)

若您要將 RAG 系統投入生產環境，請根據您的技術棧實作 `IVectorStore`。以下是針對主流關聯式資料庫的實作建議：

**1. PostgreSQL (推薦)**
PostgreSQL 擁有強大的 `pgvector` 擴充套件，是目前開源關聯式資料庫中處理向量最成熟的選擇。
*   **技術選擇**：使用 `Npgsql.EntityFrameworkCore.PostgreSQL` 搭配 `pgvector` 擴充。
*   **實作步驟**：
    1. 在資料庫中啟用套件：`CREATE EXTENSION vector;`
    2. 建立實作 `IVectorStore` 的類別（例如 `PgVectorStore`）。
    3. 在 EF Core 模型中定義 `Vector` 欄位型別，使用 `pgvector` 提供的歐式距離 (`<->`) 或餘弦相似度 (`<=>`) 運算子進行 `SearchAsync`。
*   **注入方式**：
    ```csharp
    // 註冊 EF Core DbContext，並啟用 Vector 支援
    builder.Services.AddDbContext<MyDbContext>(options => 
        options.UseNpgsql("ConnectionString", o => o.UseVector()));
    
    // 將您的實作註冊為 IVectorStore (通常使用 Scoped)
    builder.Services.AddScoped<IVectorStore, PgVectorStore>();
    ```

**2. SQL Server**
SQL Server 目前可透過外部機制或原生的 T-SQL/JSON 進行計算。
*   **技術選擇**：
    *   **方案 A (原生 T-SQL)**：將 Embedding 陣列轉換為 JSON 字串 (或正規化為多筆記錄)，透過自訂的 T-SQL 函式計算餘弦相似度 (Cosine Similarity)。適合向量維度或資料量適中的場景。
    *   **方案 B (Azure SQL)**：若您使用 Azure SQL，可搭配其原生的 Vector 支援（即將/已經推出）。
*   **實作步驟**：
    1. 建立實作 `IVectorStore` 的類別（例如 `SqlServerVectorStore`）。
    2. 若採方案 A，可在 `SearchAsync` 中透過 EF Core 的 `FromSqlRaw` 或 Dapper 呼叫撰寫好的相似度計算腳本/預存程序。
*   **注入方式**：
    ```csharp
    builder.Services.AddDbContext<MyDbContext>(options => 
        options.UseSqlServer("ConnectionString"));
    
    builder.Services.AddScoped<IVectorStore, SqlServerVectorStore>();
    ```

**3. SQLite (適合單機、桌面端或行動應用)**
SQLite 輕量且免安裝伺服器，非常適合搭配 Local LLM 實作純本地端 RAG 應用程式。
*   **技術選擇**：使用 `sqlite-vss` (Vector Search for SQLite) 擴充套件。
*   **實作步驟**：
    1. 安裝 `sqlite-vss` 擴充（注意不同 OS 的原生庫相依性）。
    2. 建立實作 `IVectorStore` 的類別（例如 `SqliteVectorStore`）。
    3. 建立包含 `vss0` 虛擬資料表的結構，並在 `SearchAsync` 中使用 `vss_search` 進行查詢。
*   **注入方式**：
    ```csharp
    // 可使用 Entity Framework Core 或 Dapper
    builder.Services.AddScoped<IVectorStore, SqliteVectorStore>();
    ```

#### 📌 基本使用方式 (與底層實作無關):
```csharp
public async Task ManageDocs(IVectorStore vectorStore, List<Document> docs)
{
    // 匯入文件 (若文件無 Embedding，Store 內部應呼叫 IEmbeddingService 產生)
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

### 7. 使用 RagEngine 統一處理流程

`RagEngine` 封裝了上述所有的複雜邏輯，提供一致的管線：

**註冊服務:**
```csharp
// 註冊所需元件與引擎
builder.Services.AddRagPipelines();
```

**使用方式:**
```csharp
public async Task ProcessRag(IRagEngine engine, List<Document> docs, string query)
{
    // 匯入資料管線 (自動處理：切塊 -> 生成 Embedding -> 儲存)
    // 預設為 Batched (重疊切塊)，您也可以指定使用 Semantic (語義切塊)
    await engine.Ingestion.IngestAsync(docs, ChunkingStrategy.Semantic);
    
    // 檢索資料管線 (自動處理：Query 轉換 -> 向量搜尋 -> 融合/Rerank)
    var results = await engine.Retrieval.RetrieveAsync(query, topK: 3);
    
    foreach (var result in results)
    {
        Console.WriteLine($"Score: {result.Score}, Content: {result.Item.Content}");
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
