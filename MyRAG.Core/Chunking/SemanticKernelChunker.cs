using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Text;
using MyRAG.Core.Extensions;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using System.Numerics.Tensors;
using System.Text;

namespace MyRAG.Core.Chunking;

/// <summary>
/// 使用 Semantic Kernel 的 TextChunker 實作的 ITextChunkingService。
/// </summary>
public class SemanticKernelChunker : ITextChunkingService
{
    private readonly Tokenizer _tokenizer;
    private readonly TextChunkingOptions _options;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;

    public SemanticKernelChunker(
        TextChunkingOptions? options = null, 
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _options = options ?? new TextChunkingOptions();
        
        // 在此使用了 TiktokenTokenizer.CreateForModel("text-embedding-ada-002")。為了讓這個 Tokenizer 在運行時能夠載入 
        // cl100k_base 編碼數據（這是 OpenAI 多數模型使用的編碼），需要安裝 Microsoft.ML.Tokenizers.Data.Cl100kBase 套件。
        // 如果沒有這個套件，在執行時可能會遇到找不到編碼數據的錯誤。
        _tokenizer = TiktokenTokenizer.CreateForModel(_options.TokenizerModelName);
        _embeddingGenerator = embeddingGenerator;
    }

    /// <inheritdoc />
    public List<List<string>> CreateBatchedChunks(string text)
    {
#pragma warning disable SKEXP0050 // TextChunker is experimental
        var lines = TextChunker.SplitPlainTextLines(text, _options.MaxTokensPerLine);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, _options.MaxTokensPerParagraph, _options.OverlapTokens);
#pragma warning restore SKEXP0050

        var batches = paragraphs.ChunkByAggregate(
            seed: 0,
            aggregator: (tokenCount, paragraph) => tokenCount + _tokenizer.CountTokens(paragraph),
            predicate: (tokenCount, index) => tokenCount <= _options.MaxTokensPerBatch && index < _options.MaxItemsPerBatch)
            .ToList();

        return batches;
    }

    /// <inheritdoc />
    public async Task<List<string>> CreateSemanticChunksAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator == null)
        {
            throw new InvalidOperationException("Embedding generator is required for semantic chunking.");
        }

        // 1. 先用較細的粒度切分句子/短項
#pragma warning disable SKEXP0050
        var sentences = TextChunker.SplitPlainTextLines(text, _options.MaxTokensPerLine);
#pragma warning restore SKEXP0050

        if (sentences.Count <= 1) return sentences;

        // 2. 取得所有句子的 Embedding
        var embeddings = await _embeddingGenerator.GenerateAsync(sentences, cancellationToken: cancellationToken);
        var embeddingVectors = embeddings.Select(e => e.Vector).ToList();

        // 3. 根據語義相似度判斷切分點
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        currentChunk.Append(sentences[0]);

        for (int i = 0; i < sentences.Count - 1; i++)
        {
            float similarity = TensorPrimitives.CosineSimilarity(embeddingVectors[i].Span, embeddingVectors[i + 1].Span);

            // 如果相似度低於閾值，視為語義轉折
            if (similarity < _options.SemanticSimilarityThreshold)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }
            else
            {
                if (currentChunk.Length > 0) currentChunk.Append(' ');
            }
            
            currentChunk.Append(sentences[i + 1]);

            // 額外保險：如果當前累積的切塊已經超過最大限制，還是要切分
            if (_tokenizer.CountTokens(currentChunk.ToString()) > _options.MaxTokensPerParagraph)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }

        return chunks;
    }

    /// <inheritdoc />
    public int CountTokens(string text) => _tokenizer.CountTokens(text);
}
