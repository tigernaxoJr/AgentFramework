using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;
using System.Text.RegularExpressions;

namespace MyRAG.Core.Retrieval;

/// <summary>
/// 實作 IQueryTransformer，使用 LLM 改寫並擴展使用者的查詢。
/// </summary>
public class QueryRewriter : IQueryTransformer
{
    private readonly IChatClient _chatClient;
    private readonly string _promptTemplate;

    public QueryRewriter(IChatClient chatClient, string? customPrompt = null)
    {
        _chatClient = chatClient;
        _promptTemplate = customPrompt ??
            "Translate and expand the following user query into a highly descriptive search query for a vector database. " +
            "Include synonyms and related technical terms. Return ONLY the final query string without any explanation or quotes.\n\n" +
            "User Query: {query}";
    }

    /// <inheritdoc />
    public async Task<string> TransformAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = _promptTemplate.Replace("{query}", query);

        // 直接使用 IChatClient 執行並取得回應
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

        var responseText = response.Text ?? string.Empty;

        // 移除 <thought> 標記及其內容
        if (responseText.Contains("<thought>"))
        {
            responseText = Regex.Replace(responseText, @"<thought>[\s\S]*?<\/thought>", "").Trim();
        }

        // 移除中繼資料行並合併結果
        var lines = responseText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("<") && !l.Contains("Role:") && !l.Contains("Task:"))
            .ToList();

        var rewrittenQuery = lines.Count > 0 ? string.Join(" ", lines) : query;

        return rewrittenQuery;
    }
}
