using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;
using System.Text.RegularExpressions;

namespace MyRAG.Core.Retrieval;

/// <summary>
/// 實作 Query Expansion，將單一查詢擴展為多個相關查詢，以提高檢索召回率。
/// </summary>
public class MultiQueryExpander
{
    private readonly IChatClient _chatClient;
    private readonly int _numQueries;

    public MultiQueryExpander(IChatClient chatClient, int numQueries = 3)
    {
        _chatClient = chatClient;
        _numQueries = numQueries;
    }

    /// <summary>
    /// 將查詢擴展為多個相似或相關的查詢。
    /// </summary>
    public async Task<List<string>> ExpandAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Generate {_numQueries} different search queries based on the original query to help retrieve relevant documents from a vector database.
Output ONLY the queries, one per line. Do not include numbering, bullets, preamble, or thinking tags.

Original query: {query}";

        // 直接使用 IChatClient 執行並取得回應
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

        var responseText = response.Text ?? string.Empty;

        // 移除 <thought> 標記及其內容（如果存在）
        if (responseText.Contains("<thought>"))
        {
            responseText = Regex.Replace(responseText, @"<thought>[\s\S]*?<\/thought>", "").Trim();
        }

        var lines = responseText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        var expandedQueries = lines
            .Select(l => Regex.Replace(l, @"^\d+\.\s*", "").Trim()) // 移除數字編號
            .Select(l => Regex.Replace(l, @"^[*+-]\s*", "").Trim()) // 移除項目符號
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("<") && !l.Contains("Role:") && !l.Contains("Task:")) // 額外過濾標記與中繼資料
            .Take(_numQueries)
            .ToList();

        // 確保原始查詢也在列表中
        if (!expandedQueries.Contains(query))
        {
            expandedQueries.Insert(0, query);
        }

        return expandedQueries;
    }
}
