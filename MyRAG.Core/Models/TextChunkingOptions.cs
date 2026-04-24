namespace MyRAG.Core.Models;

public record TextChunkingOptions
{
    public int MaxTokensPerLine { get; init; } = 100;
    public int MaxTokensPerParagraph { get; init; } = 256;
    public int OverlapTokens { get; init; } = 50;
    public int MaxTokensPerBatch { get; init; } = 8191;
    public int MaxItemsPerBatch { get; init; } = 16;
    public double SemanticSimilarityThreshold { get; init; } = 0.8;
    public string TokenizerModelName { get; init; } = "text-embedding-ada-002";
}
