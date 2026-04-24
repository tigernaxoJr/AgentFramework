# 新增 Local Foundry Embedding (WinML) 專案架構與實作建議

為了在您的 RAG 框架中引入基於 `Microsoft.AI.Foundry.Local.WinML` 的本地化 Embedding 模型，我們需要確保專案架構的**高內聚、低耦合**特性。`MyRAG.Core` 已經定義了 `IEmbeddingService`，這為整合各種不同的 Embedding Provider (如 OpenAI, Local Foundry, Ollama) 奠定了很好的基礎。

以下是針對新增此功能的架構建議與實作指南：

## 一、 專案架構建議 (Architecture Suggestions)

### 1. 保持 MyRAG.Core 的純淨性 (Clean Core)
**強烈建議不要**直接將 `Microsoft.AI.Foundry.Local.WinML` 套件安裝在 `MyRAG.Core` 專案中。
*   **原因**：該套件依賴於特定的底層組件（如 ONNX Runtime、Windows SDK/WinML 等），這會導致 Core 變得臃腫，並限制其跨平台彈性。Core 應該只保留抽象介面（`IEmbeddingService`）和通用模型。

### 2. 建立專屬的 Provider 專案 (New Integration Project)
建立一個全新的類別庫專案來專門實作這個功能。
*   **專案名稱建議**：`MyRAG.Embeddings.FoundryLocal` 或 `MyRAG.Providers.FoundryLocal`。
*   **相依性**：
    *   參考專案：`MyRAG.Core`
    *   NuGet 套件：`Microsoft.AI.Foundry.Local.WinML` 以及 `Microsoft.Extensions.DependencyInjection.Abstractions`（用於 DI 擴充）。

---

## 二、 實作修改建議 (Implementation Details)

### 1. 實作 IEmbeddingService 介面
在新專案 `MyRAG.Embeddings.FoundryLocal` 中，建立一個實作 `IEmbeddingService` 的類別。

```csharp
using MyRAG.Core.Interfaces;
using Microsoft.Extensions.AI;
// using Microsoft.AI.Foundry.Local.WinML... 引用實際的 Local Generator

namespace MyRAG.Embeddings.FoundryLocal;

public class FoundryLocalEmbeddingService : IEmbeddingService
{
    // 如果 Foundry Local 提供的是實作了 Microsoft.Extensions.AI 的 IEmbeddingGenerator
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public FoundryLocalEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<List<Embedding<float>>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default)
    {
        // 呼叫底層 WinML Embedding Generator
        var results = await _embeddingGenerator.GenerateAsync(chunks, cancellationToken: cancellationToken);
        return results.ToList();
    }

    public async Task<List<List<Embedding<float>>>> GenerateBatchedEmbeddingsAsync(IEnumerable<IEnumerable<string>> batchedChunks, CancellationToken cancellationToken = default)
    {
        var resultList = new List<List<Embedding<float>>>();
        foreach (var batch in batchedChunks)
        {
            var batchResult = await GenerateEmbeddingsAsync(batch, cancellationToken);
            resultList.Add(batchResult);
        }
        return resultList;
    }
}
```
*(備註: 實際程式碼需依據 `Microsoft.AI.Foundry.Local.WinML` 官方 API 進行初始化與微調)*

### 2. 定義 Options 模型 (Configuration)
在地端模型載入時，通常需要指定模型檔案路徑。建議建立專屬的 Options 類別。

```csharp
namespace MyRAG.Embeddings.FoundryLocal;

public class FoundryLocalEmbeddingOptions
{
    /// <summary>
    /// 模型存放的本機絕對/相對路徑 (例如 ONNX 檔案位置)
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;
}
```

### 3. 提供 Dependency Injection (DI) 擴充方法
為了讓您的應用程式（如 Console, Web API, WPF）能輕鬆註冊，請實作 DI 擴充方法。這可以放在這個新專案內。

```csharp
using Microsoft.Extensions.DependencyInjection;
using MyRAG.Core.Interfaces;

namespace MyRAG.Embeddings.FoundryLocal.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFoundryLocalEmbeddings(
        this IServiceCollection services, 
        Action<FoundryLocalEmbeddingOptions> setupAction)
    {
        var options = new FoundryLocalEmbeddingOptions();
        setupAction?.Invoke(options);

        services.Configure(setupAction);

        // 初始化底層 Foundry Local Embedding Generator 
        // 假設 foundry 有提供類似的工廠方法或直接實例化
        // var generator = new LocalEmbeddingGenerator(options.ModelPath);
        
        // 註冊作為 Singleton 或以 Factory 模式註冊
        // var service = new FoundryLocalEmbeddingService(generator);
        // services.AddSingleton<IEmbeddingService>(service);
        
        // 或者動態解析
        // services.AddSingleton<IEmbeddingService, FoundryLocalEmbeddingService>();

        return services;
    }
}
```

---

## 三、 給主應用程式 (Host App) 的修改建議

當您的應用主程式（例如 Console App 或 API）希望更換為「地端 WinML 模型」時，只需要：

1.  **安裝您的套件/參考專案**：`MyRAG.Embeddings.FoundryLocal`。
2.  **修改 DI 註冊段落** (`Program.cs` / `Startup.cs`)：

```csharp
// 舊的 (如果本來是用 OpenAI):
// services.AddOpenAIEmbeddings();

// 新的：引入您的 Foundry Local 擴充方法
services.AddFoundryLocalEmbeddings(options => 
{
    options.ModelPath = @"C:\Models\bge-micro-v2-onnx"; // 設定模型路徑
});
```
這種**插拔式 (Pluggable)** 設計確保您的 `MyRAG.Core` 業務邏輯及其他切塊器元件等完全無須修改即可順暢轉換到地端模型。

## 總結
1. **保持 Core 純淨**：不要修改 `MyRAG.Core` 專案本身，避免引入過重的原生依賴（如 ONNX 相關引擎）。
2. **建立擴充專案**：建立新專案 `MyRAG.Embeddings.FoundryLocal` 以隔離依賴。
3. **實作介面**：在新專案實作 `IEmbeddingService` 並串接 `Microsoft.AI.Foundry.Local.WinML` API。
4. **注入擴充**：提供 `AddFoundryLocalEmbeddings()` 擴充方法以方便外層 Console/WebAPI 等 Host 專案使用。
