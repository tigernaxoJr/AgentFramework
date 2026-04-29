using Microsoft.Extensions.AI;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.Numerics.Tensors;

namespace MyRAG.Embeddings.Onnx;

/// <summary>
/// 基於 ONNX Runtime 與 DirectML 的 Embedding 生成器實作。
/// 支援 iGPU (如 Radeon 780M) 加速。
/// </summary>
public sealed class OnnxEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private bool _disposed;

    /// <summary>
    /// 初始化新的 <see cref="OnnxEmbeddingGenerator"/> 實例。
    /// </summary>
    /// <param name="modelPath">ONNX 模型檔案路徑。</param>
    /// <param name="tokenizerJsonPath">tokenizer.json 檔案路徑。</param>
    /// <param name="useGPU">是否嘗試使用 DirectML GPU 加速。</param>
    public OnnxEmbeddingGenerator(string modelPath, string tokenizerJsonPath, bool useGPU = true)
    {
        // 1. 初始化 Tokenizer
        if (!File.Exists(tokenizerJsonPath))
            throw new FileNotFoundException("找不到 Tokenizer 設定檔。", tokenizerJsonPath);

        // NOTE: 在 Microsoft.ML.Tokenizers 2.0.0 中，直接從 tokenizer.json 載入可能需要更具體的類別。
        // 暫時使用 TiktokenTokenizer 作為替代，實務上應根據模型類型 (如 Qwen) 選擇適當的載入方式。
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4"); 

        // 2. 設定 ONNX Session
        var options = new SessionOptions();
        if (useGPU)
        {
            try
            {
                options.AppendExecutionProvider_DML(0); // 使用 DirectML (AMD 780M 適用)
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                options.EnableMemoryPattern = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU 加速啟動失敗: {ex.Message}，切換回 CPU 模式。");
            }
        }

        if (!File.Exists(modelPath))
            throw new FileNotFoundException("找不到 ONNX 模型檔案。", modelPath);

        _session = new InferenceSession(modelPath, options);
    }

    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = new GeneratedEmbeddings<Embedding<float>>();

        foreach (var text in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var embeddingVector = GetEmbedding(text);
            result.Add(new Embedding<float>(embeddingVector));
        }

        return await Task.FromResult(result);
    }

    private float[] GetEmbedding(string text)
    {
        // 3. Tokenization
        var ids = _tokenizer.EncodeToIds(text).Select(x => (long)x).ToArray();
        var mask = new long[ids.Length];
        Array.Fill(mask, 1L);

        // 建立輸入 Tensor (Shape: [1, sequence_length])
        var shape = new int[] { 1, ids.Length };
        var inputIdsTensor = new DenseTensor<long>(ids.AsMemory(), shape);
        var attentionMaskTensor = new DenseTensor<long>(mask.AsMemory(), shape);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        // 檢查模型是否需要 token_type_ids
        if (_session.InputMetadata.ContainsKey("token_type_ids"))
        {
            var typeIds = new long[ids.Length];
            inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(typeIds.AsMemory(), shape)));
        }

        // 檢查模型是否需要 position_ids (Qwen 等模型通常需要)
        if (_session.InputMetadata.ContainsKey("position_ids"))
        {
            var posIds = new long[ids.Length];
            for (int i = 0; i < ids.Length; i++) posIds[i] = i;
            inputs.Add(NamedOnnxValue.CreateFromTensor("position_ids", new DenseTensor<long>(posIds.AsMemory(), shape)));
        }

        // 檢查並提供 past_key_values (部分 ONNX 匯出模型強制要求)
        // 針對 Embedding 任務，我們提供空的 Tensor [1, 8, 0, 128]
        foreach (var inputName in _session.InputMetadata.Keys.Where(k => k.StartsWith("past_key_values")))
        {
            var emptyShape = new int[] { 1, 8, 0, 128 }; // 根據 Qwen3 結構調整
            var emptyTensor = new DenseTensor<float>(new float[0], emptyShape);
            inputs.Add(NamedOnnxValue.CreateFromTensor(inputName, emptyTensor));
        }

        // 4. 執行推理
        using var results = _session.Run(inputs);

        // 取得輸出 (通常是第一個輸出，或是名為 "last_hidden_state" / "sentence_embedding")
        var outputEntry = results.FirstOrDefault() ?? throw new InvalidOperationException("模型未傳回任何輸出。");
        var outputTensor = outputEntry.AsTensor<float>();

        // 5. Pooling & Normalization
        float[] vector;
        if (outputTensor.Dimensions.Length == 3) // [Batch, Seq, Dim]
        {
            vector = MeanPooling(outputTensor, mask);
        }
        else // [Batch, Dim]
        {
            vector = outputTensor.ToArray();
        }

        return L2Normalize(vector);
    }

    private float[] MeanPooling(Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> lastHiddenState, long[] mask)
    {
        // 實作 Mean Pooling (加權平均)
        int seqLen = (int)lastHiddenState.Dimensions[1];
        int dim = (int)lastHiddenState.Dimensions[2];
        float[] mean = new float[dim];
        float validCount = 0;

        for (int i = 0; i < seqLen; i++)
        {
            if (mask[i] == 0) continue;
            validCount++;
            for (int d = 0; d < dim; d++)
            {
                mean[d] += lastHiddenState[0, i, d];
            }
        }

        if (validCount > 0)
        {
            for (int d = 0; d < dim; d++) mean[d] /= validCount;
        }

        return mean;
    }

    private float[] L2Normalize(float[] vector)
    {
        // 使用 System.Numerics.Tensors 進行高效能 L2 正規化
        float sumOfSquares = TensorPrimitives.SumOfSquares(vector);
        float norm = MathF.Sqrt(sumOfSquares);
        if (norm > 0)
        {
            TensorPrimitives.Divide(vector, norm, vector);
        }
        return vector;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _session.Dispose();
            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
