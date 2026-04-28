# MyRAG.VectorDb.SqlServer

這是 `MyRAG.Core` 的 SQL Server 向量資料庫實作擴充專案。

## 特點

- **關聯式資料庫整合**：將向量資料儲存在傳統的 SQL Server 中，方便與現有業務資料整合。
- **自動 Schema 建立**：啟動時自動檢查並建立必要的資料表與索引。
- **靈活的向量運算**：使用 `VARBINARY(MAX)` 儲存向量，並透過 C# 或 T-SQL 進行相似度計算。

## 安裝與配置

### 1. 專案依賴
- `Microsoft.Data.SqlClient`
- `Dapper`
- `MyRAG.Core`

### 2. 註冊服務
在 `Program.cs` 中：

```csharp
using MyRAG.VectorDb.SqlServer.Extensions;

// 註冊 SQL Server 向量資料庫
builder.Services.AddSqlServerVectorStore(
    connectionString: "Server=(localdb)\\mssqllocaldb;Database=MyRagDb;Trusted_Connection=True;",
    tableName: "MyVectors"
);
```

## 注意事項

1. **效能**：此實作目前使用暴力掃描 (Flat Search) 配合 C# 端計算相似度。對於萬級以上的資料量，建議配合 SQL Server 2022+ 的向量搜尋功能或建立專用索引。
2. **連線字串**：請確保執行帳號具備建立資料表 (Create Table) 的權限。
