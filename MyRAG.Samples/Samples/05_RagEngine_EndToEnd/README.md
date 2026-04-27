# 範例 05：RagEngine 端對端流程 (End-to-End)

## 概覽

| 項目 | 說明 |
|------|------|
| **檔案** | `RagEngineEndToEndExample.cs` |
| **核心介面** | `IRagEngine`、`IIngestionPipeline`、`IRetrievalPipeline` |
| **需要 Embedding API** | ✅ **是** |
| **難度** | ⭐⭐⭐ 中級 |

---

## 學習目標

- 理解 `IRagEngine` 的完整管線設計
- 學習用單一入口整合「資料匯入」與「資料檢索」兩條管線
- 了解 `IIngestionPipeline`（自動切塊 → Embedding → 存入 VectorStore）
- 了解 `IRetrievalPipeline`（查詢 → 向量搜尋 → 排名融合 → 回傳）

---

## 核心概念：RagEngine 架構

`RagEngine` 是 MyRAG.Core 的最高層抽象，封裝了完整的 RAG 管線：

```
┌─────────────────────────────────────────────────────────┐
│                       IRagEngine                         │
│                                                          │
│  ┌──────────────────────┐  ┌─────────────────────────┐  │
│  │   IIngestionPipeline  │  │  IRetrievalPipeline     │  │
│  │                      │  │                         │  │
│  │  Input: Document[]   │  │  Input: query string    │  │
│  │      ↓               │  │      ↓                  │  │
│  │  ITextChunkingService│  │  IQueryTransformer      │  │
│  │  (切塊)              │  │  (查詢優化，選填)        │  │
│  │      ↓               │  │      ↓                  │  │
│  │  IEmbeddingService   │  │  IVectorStore.Search    │  │
│  │  (向量生成)          │  │  (向量搜尋)             │  │
│  │      ↓               │  │      ↓                  │  │
│  │  IVectorStore.Upsert │  │  IRankFusion            │  │
│  │  (存入資料庫)        │  │  (RRF 排名融合，選填)   │  │
│  │      ↓               │  │      ↓                  │  │
│  │  Output: void        │  │  IReranker              │  │
│  │                      │  │  (重排序，選填)         │  │
│  │                      │  │      ↓                  │  │
│  │                      │  │  Output: RankedItem[]   │  │
│  └──────────────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 兩條管線詳解

### Ingestion Pipeline（資料匯入管線）

```csharp
await engine.Ingestion.IngestAsync(documents, ChunkingStrategy.Batched);
```

**流程：**
1. 接受 `List<Document>` 作為輸入（可以是未切塊的原始長文件）
2. 使用 `ITextChunkingService` 將每份文件切成 chunks
3. 對每個 chunk 呼叫 `IEmbeddingService` 產生向量
4. 批次呼叫 `IVectorStore.UpsertAsync` 存入資料庫

**切塊策略（`ChunkingStrategy`）：**

| 值 | 說明 |
|----|------|
| `ChunkingStrategy.Batched` | 重疊切塊（預設，速度快，無需 Embedding） |
| `ChunkingStrategy.Semantic` | 語義切塊（需額外呼叫 Embedding API 進行切塊分析） |

### Retrieval Pipeline（資料檢索管線）

```csharp
var results = await engine.Retrieval.RetrieveAsync(query, topK: 3);
```

**流程：**
1. 接受查詢字串
2. 若有 `IQueryTransformer`（QueryRewriter 或 HyDE），先優化查詢
3. 呼叫 `IVectorStore.SearchAsync` 進行向量搜尋
4. 若有 `IRankFusion`，融合多路結果
5. 若有 `IReranker`，重新排序
6. 回傳 `IEnumerable<RankedItem<Document>>`

---

## RankedItem 回傳格式

```csharp
// RetrievalPipeline 回傳的每一筆結果
public class RankedItem<T>
{
    public T Item { get; }        // 原始 Document 物件
    public double Score { get; }  // 相似度分數（越高越相關）
}
```

實際使用：

```csharp
var results = await engine.Retrieval.RetrieveAsync(query, topK: 3);

foreach (var ranked in results)
{
    Console.WriteLine($"Score: {ranked.Score:F4}");
    Console.WriteLine($"Content: {ranked.Item.Content}");
    Console.WriteLine($"Source: {ranked.Item.Source}");
}
```

---

## 此範例的測試內容

### 匯入文件

| 文件 | 主題 |
|------|------|
| `e2e-001` | LLM 原理與 RAG 技術介紹（較長文本，含多個語意段落） |
| `e2e-002` | MyRAG.Core 框架設計說明 |

兩份文件都是**原始長文本**，由 `IIngestionPipeline` 自動切塊後存入。

### 測試查詢

| 查詢 | 預期最相關 chunk |
|------|----------------|
| `RAG 如何解決幻覺問題？` | `e2e-001` 的相關片段 |
| `MyRAG 框架如何切換向量資料庫？` | `e2e-002` 的相關片段 |

---

## 執行範例輸出

```
══════════════════════════════════════════════════════════════
  範例 05：RagEngine 端對端流程 (End-to-End)
══════════════════════════════════════════════════════════════
  ⚠ 此範例需要 Embedding API 連線。

  ▶ 步驟 1 / 2：執行資料匯入管線 (Ingestion Pipeline)

  待匯入文件數: 2
  切塊策略: Batched (預設，帶重疊)
  ✔ 資料匯入完成！耗時：2134 ms

  ▶ 步驟 2 / 2：執行資料檢索管線 (Retrieval Pipeline)

  🔍 查詢："RAG 如何解決幻覺問題？"
  耗時: 421 ms，取回 3 筆
  [1] (score: 0.8932) RAG（Retrieval-Augmented Generation）是一種結合資訊...
  [2] (score: 0.7641) 大型語言模型（LLM）是以 Transformer 架構為基礎...
  [3] (score: 0.7203) GPT-4、Claude、Gemini 是目前最知名的 LLM 代表...

  🔍 查詢："MyRAG 框架如何切換向量資料庫？"
  ...

  ✔ 端對端範例完成！
```

---

## 與直接使用 IVectorStore 的差異

| 比較項目 | 直接使用 IVectorStore | 使用 IRagEngine |
|---------|---------------------|----------------|
| 切塊 | 需手動呼叫 ITextChunkingService | ✅ 自動 |
| Embedding 生成 | 需手動呼叫 IEmbeddingService | ✅ 自動 |
| 查詢優化 | 需手動實作 | ✅ 透過 IQueryTransformer（選填） |
| 排名融合 | 需手動實作 | ✅ 透過 IRankFusion（選填）|
| 程式碼複雜度 | 高（需串接多個介面） | 低（單一入口） |
| 彈性 | 高（可精細控制每個步驟） | 中（依管線預設行為） |

---

## 進階使用：加入 QueryRewriter

若要進一步提升查詢品質，可在 DI 中加入查詢轉換器（需要 `IChatClient`）：

```csharp
// 加入 Query Rewriter（重寫/擴充使用者查詢）
services.AddQueryRewriter();

// 或 HyDE（生成假設文件，再以假設文件作為查詢向量）
services.AddHyDETransformer();
```

---

## 延伸閱讀

- 上一步：[範例 04 - LanceDB 語義搜尋](../04_LanceDB_Retrieval/README.md)
- 相關介面：`MyRAG.Core/Interfaces/IRagEngine.cs`、`IIngestionPipeline.cs`、`IRetrievalPipeline.cs`
- 管線實作：`MyRAG.Core/Pipelines/`
