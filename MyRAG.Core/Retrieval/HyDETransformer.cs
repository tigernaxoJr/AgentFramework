using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MyRAG.Core.Retrieval;

/// <summary>
/// Implementation of IQueryTransformer that generates a hypothetical document (HyDE) to improve retrieval.
/// </summary>
public class HyDETransformer : IQueryTransformer
{
    private readonly IChatClient _chatClient;
    private readonly string _promptTemplate;

    public HyDETransformer(IChatClient chatClient, string? customPrompt = null)
    {
        _chatClient = chatClient;
        _promptTemplate = customPrompt ??
            "Provide a hypothetical, detailed answer to the following question. " +
            "The answer should sound like a passage from a technical manual or a textbook to help in vector search. " +
            "Question: {query}";
    }    

    /// <inheritdoc />
    public async Task<string> TransformAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = _promptTemplate.Replace("{query}", query);

        // 建立 Agent
        var agent = _chatClient.AsAIAgent(instructions: "You are helpful");

        // 執行並取得回應
        AgentResponse response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        // 新版直接用 AgentResponse.Text
        var rewrittenQuery = response.Text?.Trim() ?? query;

        return rewrittenQuery;
    }
}
