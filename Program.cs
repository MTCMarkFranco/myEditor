using System.IO;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace myEditor
{
    internal class Program
    {
        static Kernel sk = null;
        static async Task Main(string[] args)
        {
            bool flag = false;
            StreamWriter writer = null;
  
            // Get out Secrets from Secret Store...
            var configBuilder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            string YourSubscriptionKey = configBuilder["SUB_KEY"] ?? string.Empty;
            string YourServiceRegion = configBuilder["SVC_REGION"] ?? string.Empty;
            string aoiEndPoint = configBuilder["AOI_ENDPOINT"] ?? string.Empty;
            string aoiKey = configBuilder["AOI_KEY"] ?? string.Empty;

            // Speech SDK
            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            var recognizer = new SpeechRecognizer(config);

            // Semantic Kernel and Generative reasoning
            var skBuilder = Kernel.CreateBuilder();

            // Add the Azure OpenAI Chat Completion
            skBuilder.AddAzureOpenAIChatCompletion("GPT4", aoiEndPoint, aoiKey);
            Program.sk = skBuilder.Build();
                        
            // Start Main Loop
            while (!flag)
            {
                var result = await recognizer.RecognizeOnceAsync();
                
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    // Get intent of the text. If the intent is to stop transcription then set Flag = true
                    if (await shouldStopTranscription(result.Text))
                    {
                        Console.WriteLine("Shutting down transcription...s Good Bye!");
                        flag = true;
                    }
                    else
                    {
                        Console.WriteLine(result.Text);
                        
                        if (writer == null)
                        {
                            writer = new StreamWriter("recording.txt", append: true) { AutoFlush = true };
                        }

                        writer.WriteLine(result.Text);
                    }
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"No transcribable speech detected. Listening...");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    flag = true;
                }
            }

            if (writer != null)
            {
                writer.Close();
            }
        }

      
        private async static Task<bool> shouldStopTranscription(string text)
        {
            ChatHistory history = [];
            history.AddSystemMessage(@"You're a virtual assistant that will determine 
                                       if the user would like to stop transcription.
                                       return a Json Object with a 'stop' key set to 
                                       true if the user wants to stop transcription.
                                       Otherwise, return a Json Object with a 'stop' 
                                       key set to false.");
            history.AddUserMessage(text);
            
            var chatCompletionService = Program.sk.GetRequiredService<IChatCompletionService>();

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                MaxTokens = 200,
                Temperature = 0.1,
            };

            var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: Program.sk);

            await foreach (var message in response)
            {
                if (message.Content != null && message.Content.Length > 0)
                {
                    var content = message.Content[0]?.ToString();
                    if (content != null)
                    {
                        var stop = JsonSerializer.Deserialize<Dictionary<string, bool>>(content)?["stop"];
                        if (stop.HasValue && stop.Value)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;

        }
    }
}
