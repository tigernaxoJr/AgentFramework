using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Interfaces;
using MyRAG.Reranking.Onnx;

namespace MyRAG.Reranking.Onnx.Extensions;

public static class RerankerServiceCollectionExtensions
{
    /// <summary>
    /// 註冊基於 ONNX 的 Reranker 服務。
    /// </summary>
    /// <param name="services">服務集合。</param>
    /// <param name="modelPath">ONNX 模型路徑。</param>
    /// <param name="tokenizerJsonPath">Tokenizer 設定檔路徑。</param>
    /// <param name="useGPU">是否使用 GPU 加速。</param>
    /// <returns>服務集合。</returns>
    public static IServiceCollection AddOnnxReranker(
        this IServiceCollection services, 
        string modelPath, 
        string tokenizerJsonPath, 
        bool useGPU = true)
    {
        services.AddSingleton<IReranker>(sp => 
            new OnnxReranker(modelPath, tokenizerJsonPath, useGPU));
            
        return services;
    }
}
