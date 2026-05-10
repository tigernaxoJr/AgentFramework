# MyRAG.PracticalApp

這是一個完整的端對端 RAG（Retrieval-Augmented Generation）實際應用範例專案，旨在展示如何將 `MyRAG.Core` 框架中的各個模組串接起來，並透過設定檔（`appsettings.json`）來達到高彈性的元件抽換。

## 特色功能

本專案將複雜的 RAG 元件封裝並支援動態切換：
- **Vector DB 切換**：支援使用 SQL Server 或是 LanceDB 作為底層向量資料庫。
- **Chunking 策略切換**：可選擇標準批次切塊（Batched）或基於 ONNX 模型的語意切塊（Semantic）。
- **Query Expansion（查詢擴展）**：支援使用 Ollama 啟動本地的 LLM 進行查詢擴充，提升檢索召回率。
- **完全在地化 (Local AI)**：
  - Embedding：透過 ONNX Runtime 載入本地 Embedding 模型。
  - Reranker：透過 ONNX Runtime 載入本地 Reranker 模型進行二次排序。

## 環境準備

執行本專案前，請確認您已準備好以下環境：

1. **.NET 10 SDK** (或符合專案定義的版本)
2. **ONNX 模型檔案**：
   - 您需要下載 Embedding 模型與 Reranker 模型的 ONNX 版本與 tokenizer 檔案。
   - 範例推薦模型：`qwen3-embedding-0.6B` (1024維度) 及 `qwen3-reranker-0.6B`。
3. **Ollama (若啟用 Query Expansion)**：
   - 需安裝 Ollama 並下載對應的模型（例如：`llama3`）。
4. **SQL Server (若使用 SQL Server 向量庫)**：
   - 需備有 SQL Server 資料庫實例並提供連線字串。
   - **手動建立資料表**：本專案的 `SqlServerVectorStore` 雖然具備自動建立表的功能，但若您需要手動建立或調整結構，請參考下方 SQL：

     **傳統方式 (SQL Server 2022 及更早版本 - 本專案預設使用)：**
     ```sql
     CREATE TABLE [RagDocuments] (
         [Id] NVARCHAR(450) PRIMARY KEY,
         [Content] NVARCHAR(MAX) NOT NULL,
         [Source] NVARCHAR(MAX) NULL,
         [Metadata] NVARCHAR(MAX) NULL,
         [Embedding] VARBINARY(MAX) NOT NULL -- 儲存向量二進位資料
     );
     ```

     **原生方式 (SQL Server 2025+ / Azure SQL)：**
     若您的版本支援原生向量，建議改用 `VECTOR` 類型：
     ```sql
     CREATE TABLE [RagDocuments] (
         [Id] NVARCHAR(450) PRIMARY KEY,
         [Content] NVARCHAR(MAX) NOT NULL,
         [Source] NVARCHAR(MAX) NULL,
         [Metadata] NVARCHAR(MAX) NULL,
         [Embedding] VECTOR(1024) NOT NULL -- 1024 為 qwen3-embedding-0.6B 的維度
     );
     ```

5. **設定檔優先級**：
   - 建議將敏感資訊（如連線字串）存放在 `appsettings.local.json` 中，該檔案已被 `.gitignore` 排除，不會上傳至 Git。

## 如何設定 (`appsettings.json`)

所有的元件切換與組態皆在 `appsettings.json` 檔案內設定。請參考以下設定說明並依照您的本機環境修改：

```json
{
  "VectorDb": {
    // 向量資料庫供應商，可填入 "LanceDB" 或 "SqlServer"
    "Provider": "LanceDB",
    
    // LanceDB 的設定參數
    "LanceDB": {
      "Path": "./lancedb_data",    // 資料庫檔案存放的本機路徑
      "TableName": "documents"     // 資料表名稱
    },
    
    // SQL Server 的設定參數
    "SqlServer": {
      "ConnectionString": "Server=localhost;Database=MyRagDB;User Id=sa;Password=Your_password123;TrustServerCertificate=True;",
      "TableName": "RagDocuments"
    }
  },
  
  "QueryExpansion": {
    // 是否啟用查詢擴展
    "Enabled": true,
    // Ollama 上的模型名稱
    "ModelId": "llama3",
    // Ollama API 的端點 (預設通常是 11434)
    "Endpoint": "http://localhost:11434/"
  },
  
  "Chunking": {
    // 切塊策略。可選 "Semantic" (語意切塊) 或 "Batched" (標準長度切塊)
    // 若選 Semantic，則會使用下方的 OnnxEmbedding 進行語意分析
    "Strategy": "Semantic"
  },
  
  "OnnxEmbedding": {
    // ONNX Embedding 模型的路徑
    "ModelPath": "D:\\onnx\\qwen3-embedding-0.6B\\model_quantized.onnx",
    "TokenizerPath": "D:\\onnx\\qwen3-embedding-0.6B\\tokenizer.json",
    // 是否使用 GPU 運算 (DirectML 等)
    "UseGPU": false
  },
  
  "OnnxReranker": {
    // 是否啟用檢索後的二次排序 (Reranking)
    "Enabled": true,
    // ONNX Reranker 模型的路徑
    "ModelPath": "D:\\onnx\\qwen3-reranker-0.6B\\model_q4.onnx",
    "TokenizerPath": "D:\\onnx\\qwen3-reranker-0.6B\\tokenizer.json",
    // 是否使用 GPU 運算
    "UseGPU": false
  }
}
```

## 執行流程簡介

在 `Program.cs` 的主程式中，應用程式會執行以下操作：

1. **依賴注入初始化**：根據 `appsettings.json` 載入指定的 Vector DB、Embedding 模型、Reranker 以及 Ollama Chat Client。
2. **資料匯入 (Ingestion Pipeline)**：
   - 將測試資料使用指定的切塊策略（如 Semantic）進行切割。
   - 透過 ONNX Embedding 產生向量。
   - 存入目標資料庫（LanceDB 或 SQL Server）。
3. **資料檢索 (Retrieval Pipeline)**：
   - 接收使用者的 Query。
   - 若啟用 Query Expansion，則先由 Ollama LLM 將查詢擴展成多個相似的問句。
   - 分別進行向量檢索，取出相關的 Context。
   - 若啟用 Reranker，再利用 ONNX Reranking 模型針對這些 Context 重新打分數與排序。
   - 輸出最終排名最高的參考結果。

## 如何執行

開啟終端機並切換到本專案目錄，確認設定檔中的路徑無誤後，輸入：

```bash
dotnet run
```
