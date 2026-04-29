# MyRAG.VectorDb.LanceDB

這是 `MyRAG.Core` 的 LanceDB 向量資料庫實作擴充專案。LanceDB 是一個高效能、無伺服器 (Serverless) 的向量資料庫，基於 Lance 格式建立，非常適合用於需要磁碟持久化且高效檢索的 RAG 應用。

## 特點

- **高效能向量搜尋**：基於 Rust 核心開發，具備極高的檢索效率。
- **無伺服器架構**：無需安裝複雜的資料庫服務，只需指定本機路徑即可運作。
- **自動 Embedding 補齊**：整合 `IEmbeddingService`，在儲存文件時若缺少向量表示，會自動進行編碼。
- **支援 Upsert 去重**：在寫入相同 ID 的文件時會自動覆蓋舊資料，確保知識庫不重複。
- **儲存優化與壓縮**：提供 `OptimizeAsync` 介面，支援物理清理已刪除資料並回收磁碟空間。
- **結構化中繼資料**：支援儲存文件的 Metadata，並在檢索時還原。

## 安裝與配置

### 1. 專案依賴
本專案依賴於以下主要套件：
- `LanceDB` (v2.3.0+)
- `MyRAG.Core`

### 2. 註冊服務
在您的應用程式啟動配置中 (如 `Program.cs`)，使用擴充方法註冊 LanceDB：

```csharp
using MyRAG.VectorDb.LanceDB.Extensions;

// 註冊 LanceDB 向量資料庫
// 參數 1: 資料庫存放目錄路徑
// 參數 2: 資料表名稱 (選填，預設為 "documents")
builder.Services.AddLanceDBVectorStore("./lancedb_storage", "my_vdb_table");
```

## 使用範例

當您完成了 DI 註冊後，`IVectorStore` 將會自動被替換為 `LanceDBVectorStore`。您可以直接透過 `IRagEngine` 或 `IVectorStore` 介面進行操作：

```csharp
public class MyService
{
    private readonly IVectorStore _vectorStore;

    public MyService(IVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public async Task ProcessData()
    {
        var docs = new List<Document>
        {
            new Document { Content = "LanceDB 是基於 Rust 打造的向量資料庫。" },
            new Document { Content = ".NET 10.0 是未來開發的主流版本。" }
        };

        // 儲存文件 (會自動產生 Embedding)
        await _vectorStore.UpsertAsync(docs);

        // 搜尋最相關的文件
        var results = await _vectorStore.SearchAsync("介紹一下 LanceDB", topK: 3);
        
        foreach (var doc in results)
        {
            Console.WriteLine($"找到文件：{doc.Content}");
        }
    }
}
```

## 進階設置與操作範例

### 1. 資料表自動建立機制
`LanceDBVectorStore` 會在第一次呼叫 `UpsertAsync` 時自動檢查資料表是否存在。
- 如果資料表不存在：會根據第一批文件的 Embedding 維度自動建立具備正確 Schema 的資料表。
- 如果資料表已存在：會直接進行資料追加 (Append)。

### 2. 中繼資料 (Metadata) 的處理
中繼資料在 LanceDB 中以 JSON 字串形式儲存在 `metadata` 欄位。搜尋結果會自動將其還原回 `Dictionary<string, object>`。

```csharp
var doc = new Document 
{ 
    Content = "重要資訊",
    Metadata = new Dictionary<string, object> 
    { 
        ["Category"] = "Research",
        ["Priority"] = 1,
        ["Tags"] = new[] { "AI", "VectorDB" }
    }
};
```

### 3. 資料庫優化與空間回收 (Optimize)
由於 LanceDB 採用 MVCC 設計，刪除資料後磁碟空間不會立即釋放。您可以手動呼叫 `OptimizeAsync` 來執行壓縮：

```csharp
// 執行檔案壓縮與舊版本清理 (建議在大量匯入後呼叫)
await _vectorStore.OptimizeAsync();
```

### 4. 手動管理資料表 (自定義工具建議)
如果您需要手動建立索引或清理資料表，可以參考以下範例：

```csharp
// 建立向量索引 (建議在資料量較大時手動執行以加速搜尋)
// LanceDB 支援 IVF-PQ, HNSW 等索引
// 目前本實作採 Flat 搜尋 (精準搜尋)，適合中小規模資料
```

### 4. 向量維度與資料型別映射
本實作將資料映射為 Apache Arrow 格式：
- `id`: `StringType` (不可為空)
- `content`: `StringType` (不可為空)
- `source`: `StringType` (可為空)
- `metadata`: `StringType` (JSON)
- `vector`: `FixedSizeListType<FloatType>` (維度由 Embedding 模型決定)

## 常見問題 (FAQ)

**Q: 支援 Upsert (更新已存在的 ID) 嗎？**
A: 是的。目前的實作已內建去重邏輯，在 `UpsertAsync` 時會自動檢查並替換相同 ID 的舊文件。

**Q: 資料庫檔案在哪裡？**
A: 會儲存在您註冊服務時指定的 `dbPath` 目錄下。LanceDB 會在該目錄建立 `.lance` 檔案夾。

**Q: 如何更換資料表名稱？**
A: 在 DI 註冊時指定 `tableName` 參數即可：
```csharp
services.AddLanceDBVectorStore("./data", tableName: "my_custom_table");
```

## 注意事項

1. **環境要求**：LanceDB 依賴於原生二進位檔，目前支援 Windows (x64), Linux (x64) 與 macOS (arm64/x64)。
2. **併發存取**：LanceDB 支援多個讀取者，但在寫入時會進行鎖定，建議在單一應用程式進程中使用。
3. **向量維度一致性**：同一個資料表內的向量維度必須一致。如果您更換了 Embedding 模型（例如從 768 維換到 1536 維），請務必刪除舊的資料表或使用不同的 `tableName`。
