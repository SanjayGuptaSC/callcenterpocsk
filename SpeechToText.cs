namespace Callcentersk;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

// This class is a placeholder for the Speech to Text service
public class SpeechToText
{
    public string speechKey { get; }
    public string speechRegion { get; set; }
    public string audioFilePath { get; set; }
    public string resultText { get; set; }

    static void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
    {
        switch (speechRecognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                //Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                break;
            case ResultReason.NoMatch:
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                break;
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                //Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
        }
    }
    public SpeechToText(string key, string region, string filePath)
    {
        speechKey = key;
        speechRegion = region;
        audioFilePath = filePath;

        // This is where the work happens
        //var textResult = new String("");
        
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);        
        speechConfig.SpeechRecognitionLanguage = "en-US";

        //using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var audioConfig = AudioConfig.FromWavFileInput(filePath);
        
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var stopRecognition = new TaskCompletionSource<int>();

        speechRecognizer.Recognizing += (s, e) =>
        {
            //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
        };

        speechRecognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                resultText += e.Result.Text;
                //Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
        };

        speechRecognizer.Canceled += (s, e) =>
        {
            //Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
            }

            stopRecognition.TrySetResult(0);
        };

        speechRecognizer.SessionStopped += (s, e) =>
        {
            Console.WriteLine("\n    Session stopped event.");
            stopRecognition.TrySetResult(0);
        };

        CallSpeechRegognizer(speechRecognizer, stopRecognition);

       
    }
    // Call recognizer and wait for completion.
    async static Task CallSpeechRegognizer(SpeechRecognizer recongizer, TaskCompletionSource<int> stopRecognition){
        recongizer.StartContinuousRecognitionAsync();
             // Waits for completion. Use Task.WaitAny to keep the task rooted.
        Task.WaitAny(new[] { stopRecognition.Task });

    } 

    public string getResultText(){
        return resultText;
    }

}