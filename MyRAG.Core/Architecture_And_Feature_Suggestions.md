# MyRAG.Core 功能擴充與專案架構修改建議

根據目前的程式碼庫 `MyRAG.Core` 的內容（包含 `Embeddings`、`Text`、`Ranking` 和 `Extensions`），這個專案已經具備了 RAG (Retrieval-Augmented Generation) 系統的初步核心基礎：**文本切塊 (Chunking)**、**向量化 (Embedding)** 以及**混合搜尋結果融合 (RRF Ranking)**。

為了讓 `MyRAG.Core` 成為一個更完整、更強大且具備擴展性的 RAG 框架，以下是針對「**新功能建議**」與「**專案架構修改建議**」的詳細分析：

---

## 一、 核心功能擴充建議 (New Features)

目前的 `MyRAG.Core` 專注於資料處理的片段，還缺乏 RAG 生命週期中其他重要的環節。以下是建議加入的功能模組：

### 1. 資料讀取與文件抽象 (Document Loaders & Models)
目前 `TextChunkingService` 只吃單純的 `string`。在真實場景中，資料通常帶有後設資料 (Metadata)。

**架構設計原則：核心層 (Core) 與擴充層 (Extensions) 分離**
強烈建議**不該**在 `MyRAG.Core` 裡面實作「所有」實際解析文件的程式碼，原因如下：
1. **避免臃腫的依賴 (Dependency Bloat)**：解析 PDF 通常需要引入龐大的第三方套件（例如 `PdfPig`, `iText7`），解析 HTML 可能需要 `HtmlAgilityPack`。若全部塞在 Core 專案中，任何引用此專案的人即使只需要處理純文字，也被迫下載大量不必要的 dll。
2. **單一職責原則 (SRP)**：`MyRAG.Core` 的職責是提供 RAG 的「骨架」與「核心流程」，而非萬能的文件解析器。

*   **功能與實作建議**：
    *   **在 `MyRAG.Core` 內**：
        *   新增 `Document` 實體類別，包含 `Id`, `Content`, `Metadata` (Dictionary), `Source` 等屬性。
        *   建立 `IDocumentLoader` 介面。
        *   實作**零依賴**的讀取器：例如 `TextLoader` 或 `MarkdownLoader`（使用原生 `System.IO.File` 即可）。
    *   **建立獨立的擴充專案**：
        *   例如建立 `MyRAG.DataLoaders.Pdf` 專案，在其中實作 `PdfLoader : IDocumentLoader` 並引入所需的 PDF 解析套件。
        *   當主程式 (`MyAgentFramework`) 需要讀取 PDF 時，才額外參考這個擴充專案。這樣可以保持框架的靈活性與乾淨的依賴關係。

### 2. 向量資料庫抽象層 (Vector Store Abstraction)
目前雖然有產生 Embedding 的服務，但沒有定義如何儲存與檢索這些 Embedding。
*   **功能建議**：
    *   新增 `IVectorStore` 或 `IRetriever` 介面。
    *   定義基礎的操作如 `UpsertAsync`, `SearchAsync(query, topK)`, `DeleteAsync`。
    *   這可以讓主專案 (MyAgentFramework) 自由抽換底層的資料庫（如 Qdrant, Chroma, Redis, 甚至 in-memory）。

### 3. 更進階的切塊策略 (Advanced Chunking Strategies)
目前的 `TextChunkingService` 使用 Semantic Kernel 的實驗性功能進行段落切塊。
*   **功能建議**：
    *   **重疊切塊 (Overlap Chunking)**：在切塊時保留上一塊的部分內容（例如 50 tokens 的重疊），避免關鍵字被硬生生切斷。
    *   **語義切塊 (Semantic Chunking)**：利用輕量級 Embedding 來判斷句意轉折點，而不是單純用換行符號切。
    *   **Markdown/HTML 感知切塊**：根據 Header (`#`, `##`) 或標籤進行切塊，保留文件結構。

### 4. 查詢轉換與優化 (Query Transformation / Routing)
使用者輸入的原始 Query 通常不是最佳的檢索字串。
*   **功能建議**：
    *   **HyDE (Hypothetical Document Embeddings)**：讓 LLM 先針對 Query 瞎掰一個答案，然後把這個答案拿去 Vector Store 搜尋，通常能找到更相似的內容。
    *   **Query Rewriting / Expansion**：利用 LLM 將使用者的簡短問題擴充成包含同義詞或更完整的查詢語句。
    *   建立 `IQueryTransformer` 介面來處理這些邏輯。

### 5. 重新排序 (Re-ranking)
目前的 `ReciprocalRankFusion` (RRF) 是一種基於排名的融合演算法，適合混合搜尋 (Hybrid Search)。但要達到最好的精準度，通常需要 Cross-Encoder 進行 Re-rank。
*   **功能建議**：
    *   新增 `IReranker` 介面。
    *   可以實作接入 Cohere Rerank API、BGE-Reranker (透過 ONNX 跑在地端) 等等。Reranker 會吃 `(Query, Document)` pair 並給出精準的相關性分數。

---

## 二、 專案架構修改建議 (Architecture Refactoring)

為了讓套件更容易被其他專案引用、測試，並符合 Domain-Driven Design (DDD) 或 Clean Architecture 的精神，建議進行以下調整：

### 1. 建立明確的介面 (Interfaces)
目前 `EmbeddingService` 和 `TextChunkingService` 是具體類別 (Concrete classes)。這不利於單元測試 (Mocking) 和依賴注入 (DI)。
*   **修改建議**：
    *   提取出 `IEmbeddingService`。
    *   提取出 `ITextChunkingService`。
    *   將這些介面統一放在 `MyRAG.Core.Interfaces` (或直接放在根目錄或對應資料夾)。

### 2. 引入 Pipeline 模式 (RAG Pipeline)
RAG 的流程是高度模組化且循序漸進的。可以設計 Pipeline 模式來串接不同的元件。
*   **修改建議**：
    *   設計 **Ingestion Pipeline (資料匯入管線)**：`DocumentLoader` -> `TextChunker` -> `EmbeddingGenerator` -> `VectorStore`
    *   設計 **Retrieval Pipeline (檢索管線)**：`QueryTransformer` -> `Retriever` (Vector + Keyword) -> `RankFusion/Reranker` -> 回傳最終 `IEnumerable<Document>`
    *   建立一個 `RagEngine` 或 `RetrievalChain` 類別來編排這些流程。

### 3. 命名空間與資料夾結構重整
隨著功能增加，建議將資料夾結構定義得更清晰：
```text
MyRAG.Core/
├── Models/                 # 放置所有的 Data Transfer Objects (DTOs)
│   ├── Document.cs
│   ├── RankedItem.cs       # 從 Ranking 移過來
│   └── ChunkingOptions.cs  # 從 Text 移過來
├── Interfaces/             # 核心介面定義
│   ├── ITextChunker.cs
│   ├── IEmbeddingService.cs
│   ├── IVectorStore.cs
│   ├── IReranker.cs
│   └── IRankFusion.cs
├── Chunking/               # 取代 Text 資料夾
│   └── SemanticKernelChunker.cs # 改名以明確表達實作來源
├── Embeddings/
│   └── EmbeddingService.cs
├── Ranking/
│   └── ReciprocalRankFusion.cs
├── Retrieval/              # 新增：檢索相關邏輯
├── DataLoaders/            # 新增：文件讀取邏輯
└── DependencyInjection/    # 新增：DI 擴充方法
    └── ServiceCollectionExtensions.cs
```

### 4. 依賴注入 (DI) 的友善支援
在 `Microsoft.Extensions.DependencyInjection` 的生態系中，提供 `AddMyRagCore()` 這類的擴充方法是很好的實踐。
*   **修改建議**：
    *   新增 `ServiceCollectionExtensions.cs`，讓使用端可以輕鬆註冊：
        ```csharp
        public static IServiceCollection AddMyRagCore(this IServiceCollection services, Action<TextChunkingOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<ITextChunkingService, TextChunkingService>();
            // 註冊其他服務...
            return services;
        }
        ```

### 總結

您目前的 `MyRAG.Core` 已經有了很好的起點，並且實作了關鍵的 RRF 和 Semantic Kernel Tokenizer 整合。
下一步的重點應該是**「定義介面與抽象層」**（讓各種 VectorDB 和 LLM 能隨插即用），以及**「擴充 RAG 生命週期的前處理與後處理功能」**（Loader, Reranker, Query Transformation），將其打造成一個流水線 (Pipeline) 架構的框架。
