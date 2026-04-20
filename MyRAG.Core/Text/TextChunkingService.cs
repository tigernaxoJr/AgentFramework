using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Text;
using MyRAG.Core.Extensions;

namespace MyRAG.Core.Text;

public record TextChunkingOptions
{
    public int MaxTokensPerLine { get; init; } = 100;
    public int MaxTokensPerParagraph { get; init; } = 256;
    public int MaxTokensPerBatch { get; init; } = 8191;
    public int MaxItemsPerBatch { get; init; } = 16;
    public string TokenizerModelName { get; init; } = "text-embedding-ada-002";
}

public class TextChunkingService
{
    private readonly Tokenizer _tokenizer;
    private readonly TextChunkingOptions _options;

    public TextChunkingService(TextChunkingOptions? options = null)
    {
        _options = options ?? new TextChunkingOptions();
        _tokenizer = TiktokenTokenizer.CreateForModel(_options.TokenizerModelName);
    }

    /// <summary>
    /// Splits text into paragraphs and consolidates them into batches based on token limits.
    /// </summary>
    public List<List<string>> CreateBatchedChunks(string text)
    {
#pragma warning disable SKEXP0050 // TextChunker is experimental
        var lines = TextChunker.SplitPlainTextLines(text, _options.MaxTokensPerLine);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, _options.MaxTokensPerParagraph);
#pragma warning restore SKEXP0050

        var batches = paragraphs.ChunkByAggregate(
            seed: 0,
            aggregator: (tokenCount, paragraph) => tokenCount + _tokenizer.CountTokens(paragraph),
            predicate: (tokenCount, index) => tokenCount <= _options.MaxTokensPerBatch && index < _options.MaxItemsPerBatch)
            .ToList();

        return batches;
    }

    /// <summary>
    /// Counts tokens in a given text.
    /// </summary>
    public int CountTokens(string text) => _tokenizer.CountTokens(text);
}
