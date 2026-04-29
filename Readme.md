# MyAgentFramework & MyRAG Solution

歡迎來到 **MyAgentFramework**。這是一個基於 .NET 10 打造的現代化 AI 代理與檢索增強生成 (RAG) 框架。本解決方案旨在提供模組化、可擴展的元件，讓開發者能快速建構強大的 AI 應用。

## 🚀 專案結構

本解決方案包含以下核心專案：

### 1. [MyRAG.Core](./MyRAG.Core)
RAG 的核心引擎，定義了所有介面與基礎實作：
- **Chunking**: 提供 `ITextChunkingService`，支援語義切塊 (Semantic) 與帶重疊的批次切塊 (Batched with Overlap)。
- **Embeddings**: 整合 `IEmbeddingService`，支援 OpenAI 相容 API 與 ONNX 本地模型。
- **Retrieval**: 包含查詢轉換 (Query Rewriter, HyDE) 與多重查詢擴展 (Multi-Query Expansion)。
- **Ranking**: 實作了排名融合演算法 (如 Reciprocal Rank Fusion, RRF)。
- **Pipelines**: 提供 `IngestionPipeline` 與 `RetrievalPipeline` 以實現端對端流程。

### 2. [MyRAG.Embeddings.Onnx](./MyRAG.Embeddings.Onnx)
基於 **ONNX Runtime** 與 **DirectML** 的本地向量嵌入實作。支援 Windows 環境下的 GPU 加速 (如 AMD Radeon 780M)，實現完全離線的高效向量生成。

### 3. [MyRAG.VectorDb.LanceDB](./MyRAG.VectorDb.LanceDB)
整合 [LanceDB](https://lancedb.com/) 的向量資料庫實作。支援無伺服器 (Serverless) 架構、高效向量搜尋與磁碟持久化。

### 4. [MyRAG.VectorDb.SqlServer](./MyRAG.VectorDb.SqlServer)
為 SQL Server 2022+ 提供的向量資料庫擴充。支援將向量資料存儲於傳統關聯式資料庫中，並提供向量相似度檢索功能。

### 5. [MyRAG.Reranking.Onnx](./MyRAG.Reranking.Onnx)
基於 **ONNX Runtime** 的本地 Reranker 實作。透過 Cross-Encoder 模型對檢索結果進行精確的二次排序，顯著提升 RAG 的回答準確度。

### 5. [MyAgentFramework](./MyAgentFramework)
基於 Microsoft Agents AI 的代理框架，支援：
- 多代理協作。
- 工具調用 (Function Calling)。
- 整合 Google Gemini、OpenAI 與本地 Ollama 模型。

### 6. [MyRAG.Samples](./MyRAG.Samples)
豐富的範例程式集，包含：
- 基礎與語義切塊示範。
- LanceDB 資料匯入、去重與優化示範。
- SQL Server 向量搜尋示範。
- ONNX 本地向量生成與 Reranking 示範。
- **範例 05：黃金端對端流程** (包含語義切塊、Query Expansion、向量檢索、ONNX Rerank 與最終 Prompt 生成)。

---

## 🛠️ 快速開始

### 環境要求
- .NET 10.0+ SDK
- (選用) SQL Server 或 LocalDB
- (選用) OpenAI 相容 API Key (如 Google Gemini, OpenAI, 或 LM Studio/Ollama)

### 安裝與編譯
```bash
git clone https://github.com/your-repo/AgentFramework.git
cd AgentFramework
dotnet build
```

### 執行範例
進入 `MyRAG.Samples` 目錄並啟動：
```bash
cd MyRAG.Samples
dotnet run
```

---

## 🌟 核心功能特色

- **重疊切塊 (Overlap Chunking)**: 確保文本切分時脈絡不丟失。
- **查詢擴展 (Query Expansion)**: 透過 LLM 生成多個變體查詢，提高搜尋召回率。
- **本地 GPU 加速**: 透過 ONNX 與 DirectML 充分利用 AMD iGPU 效能，降低雲端成本。
- **重新排名 (Reranking)**: 整合 Cross-Encoder 模型，在向量搜尋後進行精確過濾。
- **儲存空間優化**: LanceDB 支援自動 Upsert 去重與 `Optimize` 磁碟空間回收功能。
- **混合檢索 (Hybrid Search)**: 結合向量搜尋與關鍵字搜尋，並透過 RRF 進行排名融合。
- **介面優先設計**: 所有儲存與編碼元件均可透過 Dependency Injection 輕鬆替換。

## 📄 授權
本專案採用 MIT 授權。

---

*Powered by .NET 10 & Microsoft Agents AI*
