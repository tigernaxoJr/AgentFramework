using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAgentFramework
{
    internal class MyOllama
    {
        public static async Task<AgentResponse> test()
        {
            var chatClient = new OllamaChatClient(
                new Uri("http://192.168.68.250:11434"),
                modelId: "gemma4:26B");

            AIAgent agent = chatClient.AsAIAgent(
                instructions: "You are a helpful assistant running locally via Ollama.");

            return await agent.RunAsync("What is the largest city in France?");

        }
    }
}
