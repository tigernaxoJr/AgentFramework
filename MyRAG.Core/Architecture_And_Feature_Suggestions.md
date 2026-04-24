# MyRAG.Core 功能擴充與專案架構修改建議

根據目前的程式碼庫 `MyRAG.Core` 的內容，這個專案已經具備了 RAG 系統的初步核心基礎、進階切塊策略以及查詢轉換優化。

為了讓 `MyRAG.Core` 成為一個更完整、更強大且具備擴展性的 RAG 框架，以下是剩餘建議加入的功能模組：

---

## 一、 核心功能擴充建議 (New Features)

### 1. 資料讀取與文件抽象 (Document Loaders & Models)
目前雖然定義了 `Document` 實體，但還缺乏各種格式的自動讀取能力。

*   **功能與實作建議**：
    *   **在 `MyRAG.Core` 內**：建立 `IDocumentLoader` 介面，並實作零依賴的讀取器（如 `TextLoader`, `MarkdownLoader`）。
    *   **建立獨立的擴充專案**：例如 `MyRAG.DataLoaders.Pdf`，避免核心專案過於臃腫。

### 2. 更進階的切塊策略 (Advanced Chunking Strategies)
*   **功能建議**：
    *   **Markdown/HTML 感知切塊**：根據 Header (`#`, `##`) 與標籤進行切塊，保留文件結構。

### 3. 重新排序 (Re-ranking)
目前已經定義了 `IReranker` 介面。
*   **功能建議**：
    *   實作接入 Cohere Rerank API、BGE-Reranker (透過 ONNX 跑在地端) 等等。Reranker 會吃 `(Query, Document)` pair 並給出精準相關性分數，這對於混合搜尋尤其重要。

---

## 二、 專案架構修改建議 (Architecture Refactoring)

### 1. 引入 Pipeline 模式 (RAG Pipeline)
RAG 的流程是高度模組化且循序漸進的。可以設計 Pipeline 模式來串接不同的元件。
*   **修改建議**：
    *   設計 **Ingestion Pipeline (資料匯入管線)**：`DocumentLoader` -> `TextChunker` -> `EmbeddingGenerator` -> `VectorStore`
    *   設計 **Retrieval Pipeline (檢索管線)**：`QueryTransformer` -> `Retriever` -> `RankFusion/Reranker`
    *   建立一個 `RagEngine` 或 `RetrievalChain` 類別來編排這些流程。

---

### 總結

您目前的 `MyRAG.Core` 已經完成了基準架構的重構，實作了關鍵的重疊切塊、語義切塊、介面抽象化，以及查詢轉換優化（Rewriter/HyDE）。
下一步的重點應該是**「實作具體的抽象層」**（例如 VectorStore 的 Provider、Loader 的實作），以及**「串接 Pipeline 模式」**，將目前的各個模組打造成一個完整的檢索管線架構。
