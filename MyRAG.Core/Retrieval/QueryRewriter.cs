using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;

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

        // 建立 Agent
        var agent = _chatClient.AsAIAgent();

        // 執行並取得回應
        AgentResponse response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        // 新版直接用 AgentResponse.Text
        var rewrittenQuery = response.Text?.Trim() ?? query;

        return rewrittenQuery;
    }
}
