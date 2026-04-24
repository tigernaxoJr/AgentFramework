# MyRAG.Core TODO

## ✅ 已完成 (Completed)

*   **基礎重構 (Architecture Refactoring)**
    *   [x] 提取模型至 `Models` 資料夾。
    *   [x] 定義核心介面 (`Interfaces`)。
    *   [x] 重整資料夾結構 (`Chunking`, `Embeddings`, `Ranking`)。
    *   [x] 實作 `AddMyRagCore` DI 擴充方法。
*   **進階切塊策略 (Advanced Chunking)**
    *   [x] 實作 **重疊切塊 (Overlap Chunking)**。
    *   [x] 實作 **語義切塊 (Semantic Chunking)** (基於 Embedding 相似度)。
*   **查詢轉換與優化 (Query Transformation)**
    *   [x] 實作 `IQueryTransformer` 介面。
    *   [x] 實作 Query Rewriter (查詢重寫/擴充)。
    *   [x] 實作 HyDE (Hypothetical Document Embeddings)。

## 🚀 待處理 (Next Steps)

*   **向量資料庫實作 (Vector Store Implementation)**
    *   [ ] 實作 `IVectorStore` 的具體 Provider (例如 Qdrant, Chroma 或 In-memory)。
*   **文件讀取器 (Document Loaders)**
    *   [ ] 建立 `IDocumentLoader` 介面。
    *   [ ] 實作 `TextLoader` 與 `MarkdownLoader`。
    *   [ ] 建立獨立擴充專案處理 PDF/HTML (預計命名為 `MyRAG.DataLoaders.Pdf`)。
*   **重新排序優化 (Re-ranking)**
    *   [ ] 實作 `IReranker` 介面。
    *   [ ] 接入 Cohere 或 BGE Reranker。
