using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyRAG.Core.DependencyInjection;
using MyRAG.VectorDb.LanceDB.Extensions;
using MyRAG.Samples.Samples;
using MyRAG.Embeddings.Onnx.Extensions;

// ── 建立 Host ───────────────────────────────────────────────────────────────
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.SetBasePath(AppContext.BaseDirectory)
           .AddJsonFile("appsettings.json", optional: false)
           .AddJsonFile("appsettings.local.json", optional: true) // 本地覆蓋（不進 git）
           .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // 1. MyRAG 核心服務（切塊、Embedding Service、RRF 排名融合）
        // 使用預設切塊選項 (MaxTokensPerParagraph=256, OverlapTokens=50)
        // 若要自訂，請在此行之前呼叫：
        //   services.AddSingleton(new TextChunkingOptions { MaxTokensPerParagraph = 200, ... });
        services.AddMyRagCore();

        // 2. Embedding Generator
        var provider = cfg["Embedding:Provider"] ?? "OpenAI";
        if (provider.Equals("Onnx", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOnnxEmbeddingGenerator(
                modelPath: cfg["OnnxEmbedding:ModelPath"]!,
                tokenizerPath: cfg["OnnxEmbedding:TokenizerPath"]!,
                useGPU: bool.Parse(cfg["OnnxEmbedding:UseGPU"] ?? "true"));
        }
        else
        {
            services.AddOpenAICompatibleEmbeddingGenerator(
                endpoint: cfg["Embedding:Endpoint"]!,
                apiKey: cfg["Embedding:ApiKey"]!,
                modelId: cfg["Embedding:ModelId"]!);
        }

        // 3. LanceDB 向量資料庫
        services.AddLanceDBVectorStore(
            dbPath: cfg["LanceDB:Path"] ?? "./lancedb_data",
            tableName: cfg["LanceDB:TableName"] ?? "documents");

        // 4. RAG 管線 (Ingestion, Retrieval, RagEngine)
        services.AddRagPipelines();

        // 5. 範例類別（以 Scoped 註冊，依賴 DI 自動組裝）
        services.AddTransient<BasicChunkingExample>();
        services.AddTransient<SemanticChunkingExample>();
        services.AddTransient<LanceDBIngestionExample>();
        services.AddTransient<LanceDBRetrievalExample>();
        services.AddTransient<RagEngineEndToEndExample>();
        services.AddTransient<OnnxEmbeddingExample>();
    })
    .Build();

// ── 選單 ────────────────────────────────────────────────────────────────────
while (true)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║          MyRAG Framework - 範例程式集                   ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  請選擇要執行的範例：");
    Console.ResetColor();
    Console.WriteLine();

    var menuItems = new[]
    {
        ("01", "基礎重疊切塊 (Batched Chunking)              [離線可執行]"),
        ("02", "語義切塊 (Semantic Chunking)                  [需要 Embedding API]"),
        ("03", "LanceDB - 資料匯入 (Ingestion)               [需要 Embedding API]"),
        ("04", "LanceDB - 語義搜尋 (Retrieval)               [需要 Embedding API]"),
        ("05", "RagEngine 端對端流程 (End-to-End)             [需要 Embedding API]"),
        ("07", "ONNX 本地向量生成 (DirectML 加速)              [離線可執行，需模型檔案]"),
        ("00", "離開")
    };

    foreach (var (key, desc) in menuItems)
    {
        Console.ForegroundColor = key == "00" ? ConsoleColor.DarkGray : ConsoleColor.Yellow;
        Console.Write($"  [{key}]");
        Console.ResetColor();
        Console.WriteLine($" {desc}");
    }

    Console.WriteLine();
    Console.Write("  輸入選項：");
    var choice = Console.ReadLine()?.Trim();
    Console.WriteLine();

    using var scope = host.Services.CreateScope();
    var sp = scope.ServiceProvider;

    try
    {
        switch (choice)
        {
            case "01":
                await sp.GetRequiredService<BasicChunkingExample>().RunAsync();
                break;
            case "02":
                await sp.GetRequiredService<SemanticChunkingExample>().RunAsync();
                break;
            case "03":
                await sp.GetRequiredService<LanceDBIngestionExample>().RunAsync();
                break;
            case "04":
                await sp.GetRequiredService<LanceDBRetrievalExample>().RunAsync();
                break;
            case "05":
                await sp.GetRequiredService<RagEngineEndToEndExample>().RunAsync();
                break;
            case "07":
                await sp.GetRequiredService<OnnxEmbeddingExample>().RunAsync();
                break;
            case "00":
            case "exit":
            case "q":
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  再見！");
                Console.ResetColor();
                return;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  無效選項，請重新輸入。");
                Console.ResetColor();
                await Task.Delay(1000);
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  ✘ 未預期的錯誤：{ex.Message}");
        Console.ResetColor();
        Console.ReadKey(intercept: true);
    }
}
