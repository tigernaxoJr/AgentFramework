# 範例 03：LanceDB 向量資料庫 - 資料匯入 (Ingestion)

## 概覽

| 項目 | 說明 |
|------|------|
| **檔案** | `LanceDBIngestionExample.cs` |
| **核心介面** | `IVectorStore` |
| **核心方法** | `UpsertAsync(IEnumerable<Document>)` |
| **後端實作** | `LanceDBVectorStore` |
| **需要 Embedding API** | ✅ **是** |
| **難度** | ⭐⭐ 初級 |

---

## 學習目標

- 理解 `IVectorStore` 介面的抽象設計
- 學習如何透過 `UpsertAsync` 批次匯入文件並**自動產生 Embedding**
- 了解 LanceDB 的資料持久化機制
- 掌握 `Document` 物件的欄位結構

---

## 核心概念：向量資料庫與 Upsert

### IVectorStore 抽象層

`IVectorStore` 是 `MyRAG.Core` 定義的核心介面，與底層資料庫技術解耦：

```csharp
public interface IVectorStore
{
    Task UpsertAsync(IEnumerable<Document> documents, CancellationToken ct = default);
    Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken ct = default);
    Task DeleteAsync(string documentId, CancellationToken ct = default);
}
```

透過依賴注入切換實作，**不需修改業務邏輯**：

```csharp
// 開發測試 → 記憶體資料庫
services.AddInMemoryVectorStore();

// 生產環境 → LanceDB（本範例）
services.AddLanceDBVectorStore("./lancedb_data");

// 生產環境 → PostgreSQL（自行實作）
services.AddScoped<IVectorStore, PgVectorStore>();
```

### Upsert 流程

`UpsertAsync` 會自動判斷文件是否已有 Embedding：

```
輸入 Documents
      │
      ▼
[Embedding == null?]
      │ Yes              │ No
      ▼                  ▼
呼叫 IEmbeddingService   直接使用現有向量
產生 Embedding
      │
      ▼
寫入 LanceDB（Apache Arrow 格式）
      │
      ▼
資料持久化至磁碟 ./lancedb_data/
```

---

## Document 物件結構

```csharp
new Document
{
    Id = "doc-001",          // 唯一識別碼（不指定則自動生成 GUID）
    Content = "文件內容",    // 主要文字內容，用於生成 Embedding
    Source = "file.txt",     // 來源（選填）：檔案路徑、URL 等
    Metadata = new()         // 附加中繼資料（選填）：任意 key-value
    {
        { "category", "technology" },
        { "lang", "zh-TW" }
    }
    // Embedding 欄位不需手動填入，UpsertAsync 會自動填充
}
```

---

## 此範例的測試資料

範例內建 **6 份涵蓋不同主題**的示範文件，用於建立小型知識庫：

| ID | 主題 | 來源 |
|----|------|------|
| `doc-001` | LanceDB 簡介 | `lancedb_intro.md` |
| `doc-002` | RAG 技術概覽 | `rag_overview.md` |
| `doc-003` | 向量 Embedding 原理 | `embedding_intro.md` |
| `doc-004` | 台積電介紹 | `tsmc_info.md` |
| `doc-005` | .NET 框架介紹 | `dotnet_intro.md` |
| `doc-006` | MyRAG.Core 框架介紹 | `myrag_overview.md` |

---

## 執行範例輸出

```
══════════════════════════════════════════════════════════════
  範例 03：LanceDB 向量資料庫 - 資料匯入 (Ingestion)
══════════════════════════════════════════════════════════════
  ⚠ 此範例需要 Embedding API 連線，並會在本地建立 LanceDB 資料庫。

  ▶ 準備匯入 6 份示範文件...
     - [doc-001] LanceDB 是一個開源的嵌入式向量資料庫...
     - [doc-002] RAG（Retrieval-Augmented Generation）是...
     ...

  ▶ 呼叫 IVectorStore.UpsertAsync()...
  後端實作: LanceDBVectorStore
  資料庫路徑: ./lancedb_data

  ✔ 成功匯入 6 份文件！耗時：1842 ms

  提示: 資料已持久化至磁碟，下次啟動程式仍可查詢。
  提示: 執行「範例 04」查看向量搜尋效果。
```

---

## 注意事項

> [!WARNING]
> 若更換 Embedding 模型（例如從 `nomic-embed-text` 改為 `text-embedding-3-small`），兩種模型的向量**維度不同**，無法混用。請先刪除 `./lancedb_data/` 目錄，再重新執行此範例重新匯入。

> [!NOTE]
> LanceDB 使用 **Upsert** 語意（Update or Insert）。重複匯入相同 `Id` 的文件，不會產生重複資料，而是會**新增**（目前實作為 Append；若需真正 Upsert 請自行實作 Delete + Insert）。

---

## 完整程式碼解說

```csharp
// 取得示範文件
var documents = LanceDBIngestionExample.GetSampleDocuments();

// 批次匯入（自動產生 Embedding）
await vectorStore.UpsertAsync(documents);

// 也可以自行預先計算 Embedding
// var embeddings = await embeddingService.GenerateEmbeddingsAsync(contents);
// doc.Embedding = embeddings[i].Vector;
// await vectorStore.UpsertAsync(documents); // 有 Embedding 則跳過重新計算
```

---

## 延伸閱讀

- 上一步：[範例 02 - 語義切塊](../02_SemanticChunking/README.md)
- 下一步：[範例 04 - LanceDB 語義搜尋](../04_LanceDB_Retrieval/README.md)（**必須先執行本範例**）
- 相關實作：`MyRAG.VectorDb.LanceDB/LanceDBVectorStore.cs`
