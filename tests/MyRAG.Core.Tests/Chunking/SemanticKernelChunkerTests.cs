using MyRAG.Core.Chunking;
using MyRAG.Core.Models;
using Xunit;

namespace MyRAG.Core.Tests.Chunking;

public class SemanticKernelChunkerTests
{
    [Fact]
    public void CountTokens_ShouldReturnCorrectCount()
    {
        // Arrange
        var chunker = new SemanticKernelChunker();
        var text = "Hello world";

        // Act
        var count = chunker.CountTokens(text);

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public void CreateBatchedChunks_ShouldReturnChunks()
    {
        // Arrange
        var options = new TextChunkingOptions
        {
            MaxTokensPerParagraph = 10,
            OverlapTokens = 2
        };
        var chunker = new SemanticKernelChunker(options);
        var text = "This is a long text that should be split into multiple chunks because it exceeds the token limit set in the options.";

        // Act
        var batches = chunker.CreateBatchedChunks(text);

        // Assert
        Assert.NotEmpty(batches);
        Assert.True(batches.Count > 0);
        Assert.All(batches, batch => Assert.NotEmpty(batch));
    }
}
