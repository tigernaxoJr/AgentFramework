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
  [05] RagEngine 端對端流程 (End-to-End)             [需要 Embedding & Chat API]
  [07] ONNX 本地向量生成 (DirectML 加速)              [離線可執行，需模型檔案]
  [08] ONNX 本地 Reranking (DirectML 加速)            [離線可執行，需模型檔案]
  [00] 離開
```

---

## ⚙️ 設定說明

### `appsettings.json`（預設值）

```json
{
  "Embedding": {
    "Provider": "Onnx",
    "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/",
    "ApiKey": "YOUR_API_KEY",
    "ModelId": "text-embedding-004"
  },
  "Chat": {
    "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/",
    "ApiKey": "YOUR_API_KEY",
    "ModelId": "gemini-1.5-flash"
  },
  "OnnxReranker": {
    "Enabled": true,
    "ModelPath": "D:\\models\\reranker\\model.onnx",
    "TokenizerPath": "D:\\models\\reranker\\tokenizer.json"
  }
}
```

> [!TIP]
> **範例 05** 同時需要 `Embedding` (向量生成) 與 `Chat` (查詢擴展與 Prompt 生成) 的 API 連線。

---

## 📚 範例一覽

| # | 名稱 | 核心技術 | 需要 API | 說明 |
|---|------|----------|----------|------------|
| [01](./Samples/01_BasicChunking/README.md) | 基礎重疊切塊 | `ITextChunkingService` | ❌ | ⭐ 從這裡開始 |
| [02](./Samples/02_SemanticChunking/README.md) | 語義切塊 | `ITextChunkingService` | ✅ | 基於 Embedding 的切塊 |
| [03](./Samples/03_LanceDB_Ingestion/README.md) | LanceDB 匯入 | `IVectorStore.UpsertAsync` | ✅ | **支援去重與 Optimize** |
| [04](./Samples/04_LanceDB_Retrieval/README.md) | LanceDB 搜尋 | `IVectorStore.SearchAsync` | ✅ | 基礎語義搜尋 |
| [05](./Samples/05_RagEngine_EndToEnd/README.md) | **黃金端對端流程** | `IRagEngine` | ✅ | **最強範例：含 Query Expansion, Rerank 與 Prompt 生成** |
| [07](./Samples/07_Onnx_Embedding/README.md) | ONNX 本地向量 | `DirectML` 加速 | ❌ | 離線 Embedding |
| [08](./Samples/08_Onnx_Reranking/README.md) | ONNX 本地 Rerank | `DirectML` 加速 | ❌ | 離線二次排序 |

---

## 🏗️ 架構說明

```
Program.cs
│
├── Host / DI Container
│   ├── AddMyRagCore()                    # 基礎組件
│   ├── AddOnnxReranker()                 # 註冊 IReranker (若啟用)
│   ├── AddLanceDBVectorStore()           # 向量資料庫
│   └── AddRagPipelines()                 # Ingestion/Retrieval/Engine
│
└── Interactive Menu
    ├── RagEngineEndToEndExample          # 注入 IRagEngine, IChatClient, IReranker?
    └── ...其他範例...
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
