using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.PracticalApp.Configuration;
using MyRAG.PracticalApp.Extensions;
using System.Diagnostics;

namespace MyRAG.PracticalApp;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Load configuration
        var config = builder.Configuration;
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

        // 註冊所有實用程式服務與依賴
        builder.Services.AddPracticalAppServices(config);

        var host = builder.Build();

        var appOptions = host.Services.GetRequiredService<RagAppOptions>();
        var ragEngine = host.Services.GetService<IRagEngine>();
        var chatClientService = host.Services.GetService<IChatClient>();
        
        if (ragEngine == null)
        {
            Console.WriteLine("[Error] 系統啟動失敗：IRagEngine 尚未註冊成功，可能缺少必要的 Embedding 服務。");
            return;
        }

        var strategy = appOptions.Chunking.Strategy?.ToLower() == "semantic" 
                       ? ChunkingStrategy.Semantic 
                       : ChunkingStrategy.Batched;

        Console.WriteLine($"[System] Chunking Strategy: {strategy}");

        // Sample Data Ingestion
        var documents = new List<Document>
        {
            new() { Id = "doc1", Content = "The LanceDB is an open-source database for vector-search built with persistent storage.", Source = "doc1" },
            new() { Id = "doc2", Content = "SQL Server is a relational database management system developed by Microsoft.", Source = "doc2" },
            new() { Id = "doc3", Content = "ONNX is an open format built to represent machine learning models.", Source = "doc3" },
            new() { Id = "doc4", Content = "RAG stands for Retrieval-Augmented Generation, combining retrieval and text generation.", Source = "doc4" }
        };

        Console.WriteLine("\n[1] Starting Ingestion Pipeline...");
        var sw = Stopwatch.StartNew();
        try 
        {
            await ragEngine.Ingestion.IngestAsync(documents, strategy);
            Console.WriteLine($"[Success] Ingested {documents.Count} documents in {sw.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Ingestion may have failed (Are ONNX models/DB correctly set up?): {ex.Message}");
        }

        Console.WriteLine("\n[2] Starting Retrieval Pipeline...");
        var query = "Tell me about vector databases and RAG.";
        Console.WriteLine($"Query: \"{query}\"");

        try 
        {
            // Query Expansion
            if (appOptions.QueryExpansion.Enabled && chatClientService != null)
            {
                Console.WriteLine("  -> Expanding query...");
                var expander = new MyRAG.Core.Retrieval.MultiQueryExpander(chatClientService, numQueries: 2);
                try 
                {
                    var expanded = await expander.ExpandAsync(query);
                    foreach (var eq in expanded)
                    {
                        Console.WriteLine($"    [Expanded]: {eq}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    [Warning] Query expansion failed (is Ollama running?): {ex.Message}");
                }
            }

            var results = await ragEngine.Retrieval.RetrieveAsync(query, topK: 3);
            Console.WriteLine($"\n[Results] Retrieved {results.Count()} documents:");
            foreach (var result in results)
            {
                Console.WriteLine($"- [{result.Score:F4}] {result.Item.Content}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Retrieval failed: {ex.Message}");
        }

        Console.WriteLine("\nApplication complete. Press any key to exit.");
        // Console.ReadKey();
    }
}
