using System.IO;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;

namespace myEditor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            bool flag = false;
  
            // Get out Secrets from Secret Store...
            var configBuilder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            string YourSubscriptionKey = configBuilder["SUB_KEY"] ?? string.Empty;
            string YourServiceRegion = configBuilder["SVC_REGION"] ?? string.Empty;
            
            // Speech SDK
            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            var recognizer = new SpeechRecognizer(config);

            // Semantic Kernel and Generative reasoning
            //var kernel = skBuilder.Build();
            
            // File Operations
            StreamWriter writer = null;

            while (!flag)
            {
                var result = await recognizer.RecognizeOnceAsync();
                
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    // Get intent of the text. If the intent is to stop transcription then set Flag = true
                    if (shouldStopTranscription(result.Text))
                    {
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

        private static bool shouldStopTranscription(string text)
        {
            return false;
        }
    }
}
