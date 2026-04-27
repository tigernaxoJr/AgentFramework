# 範例 02：語義切塊 (Semantic Chunking)

## 概覽

| 項目 | 說明 |
|------|------|
| **檔案** | `SemanticChunkingExample.cs` |
| **核心介面** | `ITextChunkingService` |
| **核心方法** | `CreateSemanticChunksAsync(string text)` |
| **需要 Embedding API** | ✅ **是** |
| **難度** | ⭐⭐ 初級 |

---

## 學習目標

- 了解「語義切塊（Semantic Chunking）」與「重疊切塊」的核心差異
- 理解如何利用 Embedding 向量偵測主題轉換點
- 觀察對「混合主題文本」的切分效果

---

## 核心概念：語義切塊 (Semantic Chunking)

**重疊切塊**依固定 Token 數機械式切分，不考慮語義。當文本主題在某個位置突然轉換時，可能造成不同主題的內容被合併在同一個 chunk 中。

**語義切塊**解決了這個問題：

1. 將文本切分為最小單位（句子）
2. 為每個句子產生 Embedding 向量
3. 計算相鄰句子之間的**餘弦相似度**
4. 當相似度**低於閾值**（`SemanticSimilarityThreshold`），判斷為「主題轉換點」並切分

```
句子: [s1] [s2] [s3]  ↕低  [s4] [s5]  ↕低  [s6] [s7]
                      切分            切分
Chunk 1: [s1][s2][s3]
Chunk 2: [s4][s5]
Chunk 3: [s6][s7]
```

### 重疊切塊 vs 語義切塊 比較

| | 重疊切塊 | 語義切塊 |
|-|---------|---------|
| 切分依據 | Token 數量（固定長度） | 句子間語義相似度 |
| 需要 Embedding | ❌ 否 | ✅ 是 |
| 速度 | 快 | 較慢（需呼叫 Embedding API） |
| 語義完整性 | 可能跨主題 | 能識別主題邊界 |
| 適合場景 | 同質性高的長文件 | 包含多個主題的文件 |

---

## 方法簽章

```csharp
// 定義於 ITextChunkingService
Task<List<string>> CreateSemanticChunksAsync(
    string text,
    CancellationToken cancellationToken = default);
```

**注意：** 回傳 `List<string>`（不是批次包裝），可直接使用。

---

## 關鍵設定（`TextChunkingOptions`）

| 屬性 | 預設值 | 說明 |
|------|--------|------|
| `SemanticSimilarityThreshold` | 0.8 | 切分閾值。值越高，切分越積極（切出更多、更小的 chunk）；值越低，切分越保守 |

> [!TIP]
> 建議 `SemanticSimilarityThreshold` 的調整範圍為 `0.7 ~ 0.9`，需依照您的文本特性與 Embedding 模型實測後調整。

---

## 前置條件

啟動 Embedding API 後，確認 `appsettings.local.json` 設定正確：

```json
{
  "Embedding": {
    "Endpoint": "http://localhost:1234/v1",
    "ApiKey": "lm-studio",
    "ModelId": "nomic-embed-text-v1.5"
  }
}
```

---

## 此範例的測試文本設計

此範例刻意使用三個**截然不同主題**的段落（AI 技術、台灣夜市、LLM），用於驗證語義切塊能否正確識別主題邊界：

```
段落 A：機器學習 / 深度學習 / CNN / NLP...    ← 主題：AI 技術基礎
段落 B：台灣夜市 / 士林 / 基隆 / 逢甲...     ← 主題：台灣飲食文化
段落 C：LLM / GPT / RAG...                   ← 主題：大型語言模型
```

理想結果應切出 **3 個 chunk**，對應 3 個主題。

---

## 執行範例輸出（理想情況）

```
══════════════════════════════════════════════════════════════
  範例 02：語義切塊 (Semantic Chunking)
══════════════════════════════════════════════════════════════
  ⚠ 此範例需要 Embedding API 連線。

  ▶ 原始文本包含混合主題（AI技術 + 台灣夜市 + LLM）
  ▶ 執行語義切塊 (CreateSemanticChunksAsync)...
  ✔ 語義切塊完成，共 3 個 chunk

  ── Chunk 1 ──
  [1] 機器學習是人工智慧的一個子領域...（AI 技術相關）

  ── Chunk 2 ──
  [2] 台灣的夜市文化非常獨特...（夜市相關）

  ── Chunk 3 ──
  [3] 大型語言模型（LLM）...（LLM 相關）

  ▶ 對照：同一文本用 Batched 切塊結果...
  ✔ Batched 切塊共 2 個 chunk（語義切塊：3 個）
```

---

## 完整程式碼解說

```csharp
// 語義切塊（非同步，需要 IEmbeddingGenerator 已注入）
var semanticChunks = await _chunkingService.CreateSemanticChunksAsync(mixedTopicText);

// 回傳直接是 List<string>，不需 SelectMany
foreach (var chunk in semanticChunks)
{
    Console.WriteLine(chunk);
}
```

---

## 延伸閱讀

- 上一步：[範例 01 - 重疊切塊](../01_BasicChunking/README.md)
- 下一步：[範例 03 - LanceDB 資料匯入](../03_LanceDB_Ingestion/README.md)
- 相關文件：`MyRAG.Core/Chunking/` — `SemanticKernelChunker.cs`
