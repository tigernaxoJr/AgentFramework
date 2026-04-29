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
        var prompt = $@"You are an AI language model assistant. Your task is to generate {_numQueries} 
different versions of the given user query to retrieve relevant documents from a vector database. 
By generating multiple perspectives on the user query, your goal is to help the user overcome some of the limitations of the distance-based similarity search. 
Provide these alternative queries separated by newlines. Do not include numbering or any other text.

Original query: {query}";

        var agent = _chatClient.AsAIAgent();
        var response = await agent.RunAsync(prompt, cancellationToken: cancellationToken);

        var lines = response.Text?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    ?? new string[] { };

        var expandedQueries = lines
            .Select(l => Regex.Replace(l, @"^\d+\.\s*", "").Trim()) // 移除可能的數字編號
            .Where(l => !string.IsNullOrWhiteSpace(l))
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
