using System;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyAgentFramework;
using dotenv.net;

// 1. 載入 .env 檔案內容到環境變數中
DotEnv.Load();

// Create an Ollama agent using Microsoft.Extensions.AI.Ollama
// Requires: dotnet add package Microsoft.Extensions.AI.Ollama --prerelease
//var result = await MyOpenAI.test();
//Console.WriteLine(result);
await MyGoogle.test();
await new TextChunkingAndEmbedding().RunAsync();
Console.ReadLine();