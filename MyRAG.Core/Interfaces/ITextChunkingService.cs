using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

public interface ITextChunkingService
{
    /// <summary>
    /// Splits text into paragraphs and consolidates them into batches based on token limits.
    /// </summary>
    List<List<string>> CreateBatchedChunks(string text);

    /// <summary>
    /// Splits text based on semantic similarity between sentences.
    /// </summary>
    Task<List<string>> CreateSemanticChunksAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts tokens in a given text.
    /// </summary>
    int CountTokens(string text);
}
