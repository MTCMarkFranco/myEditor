using System.IO;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.CognitiveServices.Speech.Audio;
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
            config.SpeechRecognitionLanguage = "en-US"; // Set the speech recognition language to English
            config.SpeechSynthesisLanguage = "fr-CA";
            config.EnableDictation();

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(config, audioConfig);

            // Semantic Kernel and Generative reasoning
            var skBuilder = Kernel.CreateBuilder();

            // Add the Azure OpenAI Chat Completion
            skBuilder.AddAzureOpenAIChatCompletion("GPT4", aoiEndPoint, aoiKey);
            Program.sk = skBuilder.Build();

            // Subscribe to events
            recognizer.Recognizing += (s, e) => 
            { 
                Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}"); 
                
                // if e.Result.Text is contains 'cancel' or 'stop' then stop transcription
                if(e.Result.Text.Contains("cancel") || e.Result.Text.Contains("stop"))
                {
                    if(shouldStopTranscription(e.Result.Text).Result)
                    {
                        flag = true;
                        // do not recognize

                    }
                }
            };
            recognizer.Recognized += (s, e) => { Console.WriteLine($"\nRECOGNIZED: Text={e.Result.Text}"); };
            recognizer.Canceled += (s, e) => { Console.WriteLine($"\nCANCELED: Reason={e.Reason}"); };
            recognizer.SessionStopped += (s, e) => { Console.WriteLine("\nSession stopped event."); };

            // Start continuous recognition
            await recognizer.StartContinuousRecognitionAsync();

            // while flag
            while (!flag)
            {
            }

            Console.WriteLine("Stopping Transcription...");
            await recognizer.StopContinuousRecognitionAsync();
            // if (writer == null)
            // {
            //     writer = new StreamWriter("recording.txt", append: true) { AutoFlush = true };
            // }

            // if (writer != null)
            // {
            //     writer.Close();
            // }
        }

      
        private async static Task<bool> shouldStopTranscription(string text)
        {
            ChatHistory history = [];
            history.AddSystemMessage(@"You're a virtual assistant that will determine 
                                       if the user would like to stop transcription.
                                       return a Json Object with a 'stop' key set to 
                                       true if the user wants to stop transcription.
                                       Otherwise, return a Json Object with a 'stop' 
                                       key set to false. Only return a valid Json Object."
                                       );
            history.AddUserMessage(text);
            
            var chatCompletionService = Program.sk.GetRequiredService<IChatCompletionService>();

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                MaxTokens = 200,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0,
            };

            var response = await chatCompletionService.GetChatMessageContentsAsync(
                               history,
                               executionSettings: openAIPromptExecutionSettings,
                               kernel: Program.sk);

            var message = response[0];

            {
                if (message.Content != null && message.Content.Length > 0)
                {
                    var content = message.Content.Length > 0 ? message.Content.Trim('`').Trim('\n').Replace("json","") : null;
                    if (content != null)
                    {
                        try
                        {
                            var stop = JsonSerializer.Deserialize<Dictionary<string, bool>>(content)?["stop"];
                            if (stop.HasValue && stop.Value)
                            {
                                return true;
                            }
                        }
                        catch {}
                        
                    }
                }
            }

            return false;

        }
    }
}
