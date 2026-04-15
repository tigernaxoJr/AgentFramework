## Getting Started with Microsoft Agents in C#
```
dotnet add package Azure.AI.Projects --prerelease
dotnet add package Azure.Identity
dotnet add package Microsoft.Agents.AI.Foundry --prerelease
dotnet add package Microsoft.Extensions.AI.Ollama --prerelease
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add package Microsoft.Agents.AI.Abstractions --prerelease
dotnet add package dotenv.net
```

## RAG
### Chunking (文本切片)
最簡單的做法是按字數切分，但為了讓 RAG 效果更好，通常會採用 「固定大小 + 重疊 (Overlap)」 的策略。
「重疊」的作用在於確保語意不會在切點被生硬地切斷，讓前後片段能保有上下文聯繫。

語意感知切分 (Recursive Character Text Splitter)
先嘗試按 段落 (\n\n) 切，不行再按 句子 (。) 切，最後才按 空白 或 字數。
```c#
using Microsoft.SemanticKernel.Text;

// 使用 Semantic Kernel 的 TextChunker
var paragraphs = TextChunker.SplitPlainTextParagraphs(new[] { article }, maxTokensPerParagraph: 500);

// or splitplaintext?
public class SKChunkingContextProvider : IContextProvider
{
    public Task<ContextData> GetContextAsync(ContextRequest request)
    {
        var chunks = TextChunker.SplitPlainText(
            request.Input,
            maxTokens: 300,
            overlapTokens: 50
        );

        var context = new ContextData();
        context.Add("chunks", chunks);

        return Task.FromResult(context);
    }
}

```

處理 台灣醫療器材法規，切片策略
|策略|建議做法|理由|
|---|---|---|
|按法規條文切|偵測 第 X 條 做為切點|法規的最小完整語意單位通常是「條」。|
|Metadata 注入|在每個 Chunk 前加上標題,例如在 Chunk 內容前手動加上 [醫療器材管理法 - 第 3 條]|這能大幅提高 LLM 檢索後的理解力。|
|Token 計算|使用 Tiktoken 估算|LLM 的限制是 Token 而非字數。C# 可以使用 `Microsoft.ML.Tokenizers` 來精確計算。|
#### 流程
處理一篇文章的流程應該是：
清理資料：移除多餘的 HTML Tag 或特殊符號。
切片：使用 TextChunker 設定 maxTokens: 512, overlap: 50。
轉換：將每個 Chunk 丟給 Embedding 模型（如 text-embedding-3-small）。
存儲：將 Id, RawContent, VectorData, SourceUrl 存入 SQL Server。
### RFF(Reciprocal Rank Fusion) 演算法
在 RRF 演算法中，我們不再關心原始的「相似度分數」（例如 0.98 或 0.85）或「關鍵字權重」（BM25），我們只關心「排名」
```c#
public List<int> CombineRRF(List<int> vectorIds, List<int> keywordIds, int k = 60)
{
    var scoreBoard = new Dictionary<int, double>();

    // 處理向量搜尋排名
    for (int i = 0; i < vectorIds.Count; i++)
    {
        int id = vectorIds[i];
        int rank = i + 1;
        scoreBoard[id] = scoreBoard.GetValueOrDefault(id, 0) + (1.0 / (k + rank));
    }

    // 處理關鍵字搜尋排名
    for (int i = 0; i < keywordIds.Count; i++)
    {
        int id = keywordIds[i];
        int rank = i + 1;
        scoreBoard[id] = scoreBoard.GetValueOrDefault(id, 0) + (1.0 / (k + rank));
    }

    // 根據 RRF 分數降冪排列，取前 5 名
    return scoreBoard.OrderByDescending(x => x.Value)
                       .Select(x => x.Key)
                       .Take(5)
                       .ToList();
}
```
```sql
WITH VectorSearch AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY VECTOR_DISTANCE(...)) as Rank
    FROM DocumentChunks
    WHERE ... -- 向量查詢
),
KeywordSearch AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY [Rank] DESC) as Rank
    FROM DocumentChunks
    WHERE CONTAINS(Content, @query) -- 全文檢索
)
SELECT TOP 5 Id, 
       SUM(1.0 / (60 + Rank)) as RRFScore
FROM (
    SELECT Id, Rank FROM VectorSearch
    UNION ALL
    SELECT Id, Rank FROM KeywordSearch
) Combined
GROUP BY Id
ORDER BY RRFScore DESC;
```

## Reference
- [MSDN](https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent?pivots=programming-language-csharp)
