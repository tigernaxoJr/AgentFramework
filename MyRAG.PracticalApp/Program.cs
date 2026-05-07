using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using MyRAG.Core.DependencyInjection;
using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using MyRAG.Embeddings.Onnx.Extensions;
using MyRAG.Reranking.Onnx.Extensions;
using MyRAG.VectorDb.LanceDB.Extensions;
using MyRAG.VectorDb.SqlServer.Extensions;
using System.Diagnostics;

namespace MyRAG.PracticalApp;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Load configuration
        var config = builder.Configuration;
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // 1. Core MyRAG Services
        builder.Services.AddMyRagCore();

        // 2. Configure Vector DB based on setting
        var vectorDbProvider = config["VectorDb:Provider"]?.ToLower();
        if (vectorDbProvider == "sqlserver")
        {
            var connStr = config["VectorDb:SqlServer:ConnectionString"];
            var tableName = config["VectorDb:SqlServer:TableName"];
            builder.Services.AddSqlServerVectorStore(connStr!, tableName!);
            Console.WriteLine("[System] Using SQL Server Vector DB");
        }
        else
        {
            var path = config["VectorDb:LanceDB:Path"];
            var tableName = config["VectorDb:LanceDB:TableName"];
            builder.Services.AddLanceDBVectorStore(path!, tableName!);
            Console.WriteLine("[System] Using LanceDB Vector DB");
        }

        // 3. Configure Embeddings (ONNX Local Model)
        builder.Services.AddOnnxEmbeddingGenerator(
            config["OnnxEmbedding:ModelPath"]!,
            config["OnnxEmbedding:TokenizerPath"]!,
            config.GetValue<bool>("OnnxEmbedding:UseGPU")
        );
        Console.WriteLine("[System] Configured ONNX Embedding Generator");

        // 4. Configure Reranker (ONNX Local Model) if enabled
        var rerankerEnabled = config.GetValue<bool>("OnnxReranker:Enabled");
        if (rerankerEnabled)
        {
            builder.Services.AddOnnxReranker(
                config["OnnxReranker:ModelPath"]!,
                config["OnnxReranker:TokenizerPath"]!,
                config.GetValue<bool>("OnnxReranker:UseGPU")
            );
            Console.WriteLine("[System] Configured ONNX Reranker");
        }

        // 5. Configure Chat Client for Query Expansion if enabled
        var qeEnabled = config.GetValue<bool>("QueryExpansion:Enabled");
        if (qeEnabled)
        {
            var endpoint = config["QueryExpansion:Endpoint"];
            var modelId = config["QueryExpansion:ModelId"];
            
            // Register an IChatClient for multi-query expansion using Ollama
            IChatClient chatClient = new OllamaChatClient(new Uri(endpoint!), modelId!);
            builder.Services.AddSingleton<IChatClient>(chatClient);
            Console.WriteLine($"[System] Configured Query Expansion with Ollama ({modelId})");
        }

        var host = builder.Build();

        var ragEngine = host.Services.GetRequiredService<IRagEngine>();
        var chatClientService = host.Services.GetService<IChatClient>();
        var chunkingStrategyStr = config["Chunking:Strategy"];
        var strategy = chunkingStrategyStr?.ToLower() == "semantic" 
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
            if (qeEnabled && chatClientService != null)
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
