using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClientModel;
using Microsoft.Extensions.AI;
using MyAgentFramework.Tools;

namespace MyAgentFramework
{
    internal class MyOpenAI
    {
        public static async Task<AgentResponse> test()
        {
            OpenAIClient client = new OpenAIClient(new ApiKeyCredential("apikey"), new OpenAIClientOptions() { 
                Endpoint = new Uri("http://192.168.68.250:11434/v1/")
            });
            var chatClient = client.GetChatClient("gemma4:26B");

            AIAgent agent = chatClient.AsAIAgent(
                instructions: "You are a helpful assistant.  Keep your answers brief.",
                name: "Joker",
                tools: [AIFunctionFactory.Create(Weather.GetWeather)]
                );

            return await agent.RunAsync("Tell me weather about 龍井.");
        }
    }
}
