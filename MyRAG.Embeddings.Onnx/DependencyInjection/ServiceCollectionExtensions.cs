using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MyRAG.Embeddings.Onnx;

namespace MyRAG.Embeddings.Onnx.Extensions;

/// <summary>
/// 提供擴充方法，用於將 ONNX Embedding 生成器註冊到 IServiceCollection 中。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 新增基於 ONNX Runtime 的 Embedding 生成器。
    /// </summary>
    /// <param name="services">要加入服務的 IServiceCollection。</param>
    /// <param name="modelPath">ONNX 模型路徑。</param>
    /// <param name="tokenizerPath">Tokenizer 設定檔路徑。</param>
    /// <param name="useGPU">是否使用 GPU 加速。</param>
    /// <returns>回傳 IServiceCollection 以支援鏈式呼叫。</returns>
    public static IServiceCollection AddOnnxEmbeddingGenerator(this IServiceCollection services, string modelPath, string tokenizerPath, bool useGPU = true)
    {
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            return new OnnxEmbeddingGenerator(modelPath, tokenizerPath, useGPU);
        });

        return services;
    }
}
