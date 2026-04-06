using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyAgentFramework
{
    internal class MyGoogle
    {
        public static async Task<AgentResponse> test()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var modelId = Environment.GetEnvironmentVariable("OPENAI_API_MODEL");

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions()
            {
                Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/v1/")
            });
            var chatClient = client.GetChatClient(modelId);

            AIAgent agent = chatClient.AsAIAgent(
                instructions: "You are a helpful assistant running locally via Ollama.",
                name: "Joker");

            return await agent.RunAsync("Tell me a joke about a pirate.");
        }
    }
}
