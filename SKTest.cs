using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace MyAgentFramework
{
    internal class SKTest
    {
        public static async Task Test()
        {
            // Populate values from your OpenAI deployment
            //var modelId = "";
            //var endpoint = "";
            //var apiKey = "";
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var modelId = Environment.GetEnvironmentVariable("OPENAI_API_MODEL");
            var endpoint = Environment.GetEnvironmentVariable("GOOGLE_OPENAI_ENDPOINT");
            modelId = "gemma4:26B-128k";
            endpoint = "http://192.168.68.250:11434/v1/";

            // Create a kernel with Azure OpenAI chat completion
            //var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
            var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey, // Ollama usually doesn't check this, but SK requires a non-null string
                endpoint: new Uri(endpoint)
            );

            // Add enterprise components
            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

            // Build the kernel
            Kernel kernel = builder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Add a plugin (the LightsPlugin class is defined below)
            kernel.Plugins.AddFromType<LightsPlugin>("Lights");

            // Enable planning
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // Create a history store the conversation
            var history = new ChatHistory();

            // Initiate a back-and-forth chat
            string? userInput;
            do
            {
                // Collect user input
                Console.Write("User > ");
                userInput = Console.ReadLine();

                // Add user input
                history.AddUserMessage(userInput);

                // Get the response from the AI
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);
            } while (userInput is not null);

        }
    }

    public class LightsPlugin
    {
        // Mock data for the lights
        private readonly List<LightModel> lights = new()
   {
      new LightModel { Id = 1, Name = "Table Lamp", IsOn = false },
      new LightModel { Id = 2, Name = "Porch light", IsOn = false },
      new LightModel { Id = 3, Name = "Chandelier", IsOn = true }
   };

        [KernelFunction("get_lights")]
        [Description("Gets a list of lights and their current state")]
        public async Task<List<LightModel>> GetLightsAsync()
        {
            return lights;
        }

        [KernelFunction("change_state")]
        [Description("Changes the state of the light")]
        public async Task<LightModel?> ChangeStateAsync(int id, bool isOn)
        {
            var light = lights.FirstOrDefault(light => light.Id == id);

            if (light == null)
            {
                return null;
            }

            // Update the light with the new state
            light.IsOn = isOn;

            return light;
        }
    }

    public class LightModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("is_on")]
        public bool? IsOn { get; set; }
    }
}
