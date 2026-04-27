# 範例 04：LanceDB 向量資料庫 - 語義搜尋 (Retrieval)

## 概覽

| 項目 | 說明 |
|------|------|
| **檔案** | `LanceDBRetrievalExample.cs` |
| **核心介面** | `IVectorStore` |
| **核心方法** | `SearchAsync(string query, int topK)` |
| **後端實作** | `LanceDBVectorStore` |
| **需要 Embedding API** | ✅ **是** |
| **前置條件** | ⚠️ **請先執行範例 03** 匯入資料 |
| **難度** | ⭐⭐ 初級 |

---

## 學習目標

- 理解向量相似度搜尋（Vector Similarity Search）的運作原理
- 學習使用 `IVectorStore.SearchAsync` 進行語義查詢
- 體會「語義搜尋」與傳統「關鍵字搜尋」的差異
- 透過互動式查詢模式即時驗證搜尋效果

---

## 核心概念：向量相似度搜尋

### 語義搜尋 vs 關鍵字搜尋

| | 關鍵字搜尋 | 語義搜尋 |
|-|-----------|---------|
| 比對方式 | 完全字串匹配 | 語意向量距離 |
| 能否找到同義詞 | ❌ 否 | ✅ 是 |
| 能否理解問句意圖 | ❌ 否 | ✅ 是 |
| 需要 Embedding | ❌ 否 | ✅ 是 |

**範例：** 用查詢「台灣半導體」，語義搜尋能找到包含「台積電」、「晶圓代工」的文件，即使文件中完全沒有「半導體」這個詞。

### 搜尋流程

```
使用者輸入查詢字串
        │
        ▼
呼叫 IEmbeddingService 產生查詢向量
        │
        ▼
在 LanceDB 中計算向量距離（近似最近鄰搜尋）
        │
        ▼
取回最相似的 Top-K 份文件
        │
        ▼
回傳 IEnumerable<Document>
```

---

## 方法簽章

```csharp
// 定義於 IVectorStore
Task<IEnumerable<Document>> SearchAsync(
    string query,               // 自然語言查詢
    int topK = 5,               // 取回最多幾份文件
    CancellationToken ct = default);
```

---

## 此範例的預設查詢

範例內建 5 個測試查詢，用於驗證從範例 03 匯入的 6 份文件的搜尋效果：

| 查詢 | 預期最相關文件 |
|------|--------------|
| `什麼是向量資料庫？` | doc-001（LanceDB 簡介）|
| `如何解決大型語言模型的幻覺問題？` | doc-002（RAG 技術）|
| `台灣半導體產業` | doc-004（台積電）|
| `.NET 跨平台開發` | doc-005（.NET 介紹）|
| `RAG 框架有哪些功能？` | doc-006（MyRAG 框架）|

---

## 執行範例輸出

```
══════════════════════════════════════════════════════════════
  範例 04：LanceDB 向量資料庫 - 語義搜尋 (Retrieval)
══════════════════════════════════════════════════════════════
  ⚠ 請先執行範例 03 匯入資料，再執行此範例。

  🔍 查詢："什麼是向量資料庫？"
  耗時: 312 ms，取回 3 筆結果
  [1] LanceDB 是一個開源的嵌入式向量資料庫，使用 Apache Arrow 格式...
      來源: lancedb_intro.md
  [2] 向量嵌入（Vector Embedding）是將文本、圖片等非結構化資料...
      來源: embedding_intro.md
  [3] MyRAG.Core 是一個為 .NET 平台設計的 RAG 框架...
      來源: myrag_overview.md

  ── 互動式查詢模式（輸入 'exit' 離開）──

  請輸入查詢語句：_
```

---

## 互動式查詢模式

完成預設查詢後，程式進入**互動式模式**，可即時輸入任意查詢並查看結果。輸入 `exit` 返回主選單。

這個功能特別適合：
- 測試不同查詢語句的效果
- 驗證切塊與索引的品質
- 調整 `topK` 參數後觀察結果差異

---

## 完整程式碼解說

```csharp
// 基本用法：取回最相似的 3 份文件
var results = await _vectorStore.SearchAsync(query, topK: 3);

foreach (var doc in results)
{
    Console.WriteLine(doc.Content);
    Console.WriteLine(doc.Source);
    // doc.Embedding 也可取得，但通常不需要顯示
}
```

---

## 效能考量

| 場景 | 典型延遲 | 說明 |
|------|---------|------|
| 查詢向量生成 | 100-500 ms | 取決於 Embedding 模型與 API 延遲 |
| LanceDB 向量搜尋 | < 10 ms | 嵌入式資料庫，極低延遲 |
| 總計（6 份文件） | ~300-600 ms | |
| 總計（10,000 份文件） | ~300-600 ms | LanceDB 使用 ANN 近似搜尋，規模擴展性佳 |

> [!TIP]
> 瓶頸通常在**查詢向量生成**（呼叫 Embedding API），而非資料庫搜尋本身。若需要低延遲，建議使用本地部署的 Embedding 模型。

---

## 延伸閱讀

- 上一步：[範例 03 - LanceDB 資料匯入](../03_LanceDB_Ingestion/README.md)
- 下一步：[範例 05 - RagEngine 端對端](../05_RagEngine_EndToEnd/README.md)
- 相關實作：`MyRAG.VectorDb.LanceDB/LanceDBVectorStore.cs`
