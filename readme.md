# Speech to Text Transcription Project

This project uses the Microsoft Cognitive Services SDK to transcribe speech to text. It continually listens through the default microphone and sends the audio to the Cognitive Services Speech service for transcription. The transcribed text is then output to the console.

## Setup

1. Clone this repository to your local machine.

2. Navigate to the project directory in your terminal.

3. Install the necessary packages by running the following commands:

```bash
dotnet add package Microsoft.CognitiveServices.Speech
dotnet add package Microsoft.Extensions.Configuration
```

4. Set up user secrets for your subscription key and service region. Replace "YourSubscriptionKey" and "YourServiceRegion" with your actual subscription key and service region:
   
```bash
dotnet user-secrets set "SUB_KEY" "YourSubscriptionKey"
dotnet user-secrets set "SVC_REGION" "YourServiceRegion"
```