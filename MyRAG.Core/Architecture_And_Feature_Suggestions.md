# MyRAG.Core 架構優化與功能擴充路線圖 (Roadmap)

根據目前的專案進度，`MyRAG.Core` 已具備 RAG 的核心基礎、進階切塊與管線 (Pipeline) 架構。為了進一步提升其生產環境的實用性與競爭力，以下是後續的開發與優化建議：

---

## 🛠️ 一、 核心功能擴充 (Core Features)

### 1. 多樣化資料讀取器 (Document Loaders)
*   **目標**：支援更多文件格式。
*   **建議項目**：
    *   **內置讀取器**：`TextLoader`, `MarkdownLoader` (零依賴)。
    *   **擴展專案**：`MyRAG.DataLoaders.Pdf`, `MyRAG.DataLoaders.Office` (基於外部庫)。
    *   **元數據提取**：在讀取時自動提取文件大小、創建日期、標題等元數據。

### 2. 進階檢索策略 (Advanced Retrieval)
*   **混合搜尋 (Hybrid Search)**：
    *   結合向量檢索 (Vector Search) 與關鍵字檢索 (BM25/FTS)。
    *   實作 **RRF (Reciprocal Rank Fusion)** 演算法進行結果融合。
*   **重新排序 (Re-ranking)**：
    *   接入 Cohere Rerank API 或 BGE-Reranker (透過 ONNX 地端執行)。
*   **元數據過濾 (Metadata Filtering)**：
    *   支援在檢索時透過 Lambda 表達式或特定語法過濾文檔（例如：`date > '2024-01-01'`）。

### 3. 切塊策略優化 (Chunking Improvements)
*   **多層次切塊 (Hierarchical Chunking)**：實作「父子文件塊」架構，檢索小塊以求精準，提供大塊給 LLM 以求完整上下文。
*   **結構感知切塊**：針對 Markdown 或 HTML 的標籤與層級進行切塊，保留文檔結構資訊。

---

## 📈 二、 可觀測性與評估 (Observability & Evaluation)

### 1. 全方位監控 (Monitoring)
*   **OpenTelemetry 整合**：整合 `Microsoft.Extensions.AI` 的 Telemetry。
    *   追蹤 (Tracing)：記錄每個 Pipeline 節點的耗時與輸入輸出。
    *   指標 (Metrics)：記錄 Token 使用量、檢索命中率、失敗率等。

### 2. 質量評估 (Evaluation)
*   建立 `MyRAG.Evaluation` 專案，參考 RAGAS 指標：
    *   **忠誠度 (Faithfulness)**：檢查答案是否來自檢索內容。
    *   **相關性 (Relevance)**：答案是否解決了問題。
    *   **檢索精度 (Context Precision)**：檢索出的內容是否真與問題相關。

---

## 🚀 三、 效能與架構優化 (Performance & Architecture)

### 1. 語義快取 (Semantic Cache)
*   實作 `ISemanticCache` 接口，儲存已處理過的相似查詢結果，減少 LLM 調用成本並加速響應。

### 2. 在地化支援 (Local-first)
*   提供基於 ONNX Runtime 或 LlamaSharp 的地端 Embedding 與 LLM 實作，滿足高隱私場景需求。

### 3. 異步與串流優化 (Async & Streaming)
*   全面支持 `IAsyncEnumerable` 串流輸出，提升使用者感知的首字響應時間。

---

## 🌐 四、 生態系統與開發體驗 (Ecosystem & DX)

### 1. 分層應用架構
*   **MyRAG.Api**：提供標準的 RESTful/gRPC 介面。
*   **MyRAG.Cli**：命令行工具，用於批次建立索引、測試切塊策略。

### 2. 代理整合 (Agent Integration)
*   更深度的與 `Semantic Kernel` 整合，使 RAG 成為 Agent 可以調用的工具 (Tools/Functions)。

---

## 📅 優先級建議 (Priority)

| 優先級 | 功能模組 | 說明 |
| :--- | :--- | :--- |
| **P0 (Critical)** | 混合搜尋 & 評估模組 | 確保檢索質量的基準。 |
| **P1 (High)** | OTEL 監控 & Metadata Filter | 生產環境必備。 |
| **P2 (Medium)** | 多層次切塊 & Semantic Cache | 優化體驗與節省成本。 |
| **P3 (Low)** | GraphRAG & CLI 工具 | 進階擴充功能。 |
