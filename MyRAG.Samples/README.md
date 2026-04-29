# MyRAG.Samples

`MyRAG.Samples` 是 **MyRAG Framework** 的官方範例程式集，透過一系列由淺入深的互動式 Console 範例，展示如何在 .NET 應用程式中使用 `MyRAG.Core` 與 `MyRAG.VectorDb.LanceDB` 建立完整的 RAG（Retrieval-Augmented Generation）系統。

---

## 🗂️ 目錄

- [前置條件](#-前置條件)
- [快速啟動](#-快速啟動)
- [設定說明](#-設定說明)
- [範例一覽](#-範例一覽)
- [架構說明](#-架構說明)
- [常見問題](#-常見問題)

---

## ✅ 前置條件

| 項目 | 要求 |
|------|------|
| .NET SDK | 10.0 或以上 |
| Embedding API | 本地：[LM Studio](https://lmstudio.ai/) / [Ollama](https://ollama.com/)，或任何 OpenAI 相容端點 |
| ONNX 模型 | 範例 07 需下載模型檔 (如 Qwen3-Embedding) 至指定路徑 |
| 範例 01, 07 | **無需 API**，可完全離線執行 (07 需要本地模型檔) |
| 範例 02-05 | 需要 Embedding API（見[設定說明](#-設定說明)） |

---

## 🚀 快速啟動

```powershell
# 1. 進入範例專案目錄
cd d:\private\AgentFramework\MyRAG.Samples

# 2. 複製並編輯本地設定（填入你的 API 端點）
# 直接編輯已有的 appsettings.local.json

# 3. 執行
dotnet run
```

程式啟動後會顯示互動式選單：

```
╔══════════════════════════════════════════════════════════╗
║          MyRAG Framework - 範例程式集                   ║
╚══════════════════════════════════════════════════════════╝

  請選擇要執行的範例：

  [01] 基礎重疊切塊 (Batched Chunking)              [離線可執行]
  [02] 語義切塊 (Semantic Chunking)                  [需要 Embedding API]
  [03] LanceDB - 資料匯入 (Ingestion)               [需要 Embedding API]
  [04] LanceDB - 語義搜尋 (Retrieval)               [需要 Embedding API]
  [05] RagEngine 端對端流程 (End-to-End)             [需要 Embedding API]
  [07] ONNX 本地向量生成 (DirectML 加速)              [離線可執行，需模型檔案]
  [00] 離開
```

---

## ⚙️ 設定說明

### `appsettings.json`（預設值，提交至 git）

```json
{
  "Embedding": {
    "Endpoint": "http://localhost:1234/v1",
    "ApiKey": "lm-studio",
    "ModelId": "text-embedding-nomic-embed-text-v1.5"
  },
  "LanceDB": {
    "Path": "./lancedb_data",
    "TableName": "documents"
  }
}
```

### `appsettings.local.json`（本地覆蓋，已加入 `.gitignore`，不會上傳）

在此填入您的實際 API 資訊，格式相同，只需覆蓋需要修改的欄位即可。

**LM Studio 設定範例：**
```json
{
  "Embedding": {
    "Endpoint": "http://localhost:1234/v1",
    "ApiKey": "lm-studio",
    "ModelId": "nomic-embed-text-v1.5"
  }
}
```

**Ollama 設定範例：**
```json
{
  "Embedding": {
    "Endpoint": "http://localhost:11434/v1",
    "ApiKey": "ollama",
    "ModelId": "nomic-embed-text"
  }
}
```

**OpenAI 設定範例：**
```json
{
  "Embedding": {
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": "sk-...",
    "ModelId": "text-embedding-3-small"
  }
}
```

**ONNX 本地加速設定範例 (Radeon 780M 適用)：**
```json
{
  "Embedding": {
    "Provider": "Onnx"
  },
  "OnnxEmbedding": {
    "ModelPath": "D:\\onnx\\qwen3-embedding-0.6B\\model_quantized.onnx",
    "TokenizerPath": "D:\\onnx\\qwen3-embedding-0.6B\\tokenizer.json",
    "UseGPU": true
  }
}
```

> [!IMPORTANT]
> 範例 03～05 會在 `./lancedb_data/` 目錄建立持久化的向量資料庫。**請先執行範例 03 匯入資料，再執行範例 04 進行查詢。**

---

## 📚 範例一覽

| # | 名稱 | 核心技術 | 需要 API | 建議閱讀順序 |
|---|------|----------|----------|------------|
| [01](./Samples/01_BasicChunking/README.md) | 基礎重疊切塊 | `ITextChunkingService.CreateBatchedChunks` | ❌ | ⭐ 從這裡開始 |
| [02](./Samples/02_SemanticChunking/README.md) | 語義切塊 | `ITextChunkingService.CreateSemanticChunksAsync` | ✅ | |
| [03](./Samples/03_LanceDB_Ingestion/README.md) | LanceDB 匯入 | `IVectorStore.UpsertAsync` | ✅ | 先於 04 執行 |
| [04](./Samples/04_LanceDB_Retrieval/README.md) | LanceDB 搜尋 | `IVectorStore.SearchAsync` | ✅ | 需先跑 03 |
| [05](./Samples/05_RagEngine_EndToEnd/README.md) | RagEngine 端對端 | `IRagEngine` 完整管線 | ✅ | |
| [07](./Samples/07_Onnx_Embedding/README.md) | ONNX 本地向量 | `OnnxEmbeddingGenerator` (DirectML) | ❌ | 適用離線環境 |

---

## 🏗️ 架構說明

```
Program.cs
│
├── Host / DI Container
│   ├── AddMyRagCore()                    # ITextChunkingService, IEmbeddingService, IRankFusion
│   ├── AddOpenAICompatibleEmbeddingGenerator()   # IEmbeddingGenerator
│   ├── AddLanceDBVectorStore()           # IVectorStore → LanceDBVectorStore
│   └── AddRagPipelines()                 # IIngestionPipeline, IRetrievalPipeline, IRagEngine
│
└── Interactive Menu
    ├── BasicChunkingExample              # 注入 ITextChunkingService
    ├── SemanticChunkingExample           # 注入 ITextChunkingService
    ├── LanceDBIngestionExample           # 注入 IVectorStore
    ├── LanceDBRetrievalExample           # 注入 IVectorStore
    ├── RagEngineEndToEndExample          # 注入 IRagEngine
    └── OnnxEmbeddingExample              # 注入 IEmbeddingService (使用 OnnxEmbeddingGenerator)
```

所有範例類別繼承自 `Infrastructure/SampleBase.cs`，提供統一的彩色 Console 輸出工具。

---

## ❓ 常見問題

**Q: 執行時出現 `Connection refused` 或 `Unable to connect`？**
> 請確認您的 Embedding API（LM Studio / Ollama）已啟動，並確認 `appsettings.local.json` 中的 `Endpoint` 設定正確。

**Q: 範例 04 沒有搜尋結果？**
> 請先執行範例 03 進行資料匯入，資料庫需有資料才能進行搜尋。

**Q: LanceDB 資料庫儲存在哪裡？**
> 預設在執行目錄下的 `./lancedb_data/` 資料夾（已加入 `.gitignore`）。

**Q: 如何更換 Embedding 模型？**
> 修改 `appsettings.local.json` 中的 `Embedding:ModelId`。注意：更換模型後，請刪除 `./lancedb_data/` 重新匯入，因為不同模型的向量維度不相容。
