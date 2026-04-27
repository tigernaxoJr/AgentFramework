# 範例 01：基礎重疊切塊 (Batched Chunking)

## 概覽

| 項目 | 說明 |
|------|------|
| **檔案** | `BasicChunkingExample.cs` |
| **核心介面** | `ITextChunkingService` |
| **核心方法** | `CreateBatchedChunks(string text)` |
| **需要 Embedding API** | ❌ **否**，完全離線執行 |
| **難度** | ⭐ 入門 |

---

## 學習目標

- 了解 `ITextChunkingService` 的基本用法
- 理解「重疊切塊（Overlap Chunking）」的原理與輸出格式
- 學會將切塊結果轉換為 `Document` 物件，準備後續存入向量資料庫

---

## 核心概念：重疊切塊 (Overlap Chunking)

將一段長文本分割成多個小段（Chunk）時，若相鄰段落之間**完全沒有重疊**，可能導致跨越切分點的句子失去上下文。

**重疊切塊**的解決方式是讓相鄰 chunk 之間保留一定數量的 Token 重疊，確保關鍵資訊不因切分而遺失語意脈絡。

```
原始文本：[A][B][C][D][E][F][G][H]
                      ↓
Chunk 1：  [A][B][C][D]
Chunk 2：        [C][D][E][F]   ← C、D 重疊
Chunk 3：              [E][F][G][H]
```

---

## 方法簽章

```csharp
// 定義於 ITextChunkingService
List<List<string>> CreateBatchedChunks(string text);
```

**回傳值說明：**
- 外層 `List<List<string>>`：**批次（Batch）** 列表，每個批次符合 Embedding API 的 Token 上限（預設 8191 tokens）
- 內層 `List<string>`：該批次內的各個 **Chunk**

若要取得所有 chunk 的平坦列表，使用 LINQ 的 `SelectMany`：

```csharp
var allChunks = chunkingService.CreateBatchedChunks(text)
                               .SelectMany(batch => batch)
                               .ToList();
```

---

## 關鍵設定（`TextChunkingOptions`）

| 屬性 | 預設值 | 說明 |
|------|--------|------|
| `MaxTokensPerParagraph` | 256 | 每個 chunk 的最大 Token 數 |
| `OverlapTokens` | 50 | 相鄰 chunk 的重疊 Token 數 |
| `MaxTokensPerBatch` | 8191 | 每個 Batch 的最大 Token 總數（對應 Embedding API 上限） |
| `MaxItemsPerBatch` | 16 | 每個 Batch 的最大 Chunk 數量 |

---

## 執行範例輸出

```
══════════════════════════════════════════════════════════════
  範例 01：基礎重疊切塊 (Batched Chunking)
══════════════════════════════════════════════════════════════

  ▶ 原始文本長度統計
  批次數量: 1
  ✔ 共切出 5 個 chunk

  [1] 台灣是一個位於東亞的島嶼，面積約 36,000 平方公里...
  [2] 台灣的地形多樣，中央山脈縱貫南北...
  ...

  ▶ 將 chunk 轉為 Document 物件...
  ✔ 成功建立 5 個 Document 物件
  範例文件 ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6
  範例 Metadata: chunk_index=0, chunk_total=5
```

---

## 完整程式碼解說

```csharp
// 1. 注入服務（透過建構函式）
public class BasicChunkingExample(ITextChunkingService chunkingService) : SampleBase

// 2. 呼叫切塊（同步，不需 Embedding API）
var batches = _chunkingService.CreateBatchedChunks(longText);

// 3. 展平所有 chunk（略過批次分組）
var allChunks = batches.SelectMany(b => b).ToList();

// 4. 轉換為 Document 物件（帶 Metadata）
var documents = allChunks.Select((chunk, idx) => new Document
{
    Content = chunk,
    Source = "my_file.txt",
    Metadata = new Dictionary<string, object>
    {
        { "chunk_index", idx },
        { "chunk_total", allChunks.Count }
    }
}).ToList();
```

---

## 延伸閱讀

- 下一步：[範例 02 - 語義切塊](../02_SemanticChunking/README.md)（需要 Embedding API）
- 若要自訂切塊大小，參考 `MyRAG.Core` 文件中的 `TextChunkingOptions`
