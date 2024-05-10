// This program simulates using Semantic Kernel to extract information from a call center conversation
// and then using that information to craft an email summary of the call.

using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Callcentersk;
using Azure.Identity;
using Azure.Core.Diagnostics;

// Setup a listener to monitor logged events.
using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger();



var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;
config.Sources.Clear();
config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
config.AddAzureKeyVault(new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"), new DefaultAzureCredential());

//OpenAI Properties for the Semantic Kernel Service
string azureOpenAIDeploymentName = config["AzureOpenAI:DeploymentName"];
string azureOpenAIEndpoint = config["AzureOpenAI:Endpoint"];
string azureOpenAIKey = config["openai-key"];

//Initialize the kernel
var kernel = Kernel.CreateBuilder()
    //Add Azure Cognitive Services Speech to Text Service
    .AddAzureOpenAIChatCompletion(
        azureOpenAIDeploymentName,  // Azure OpenAI Deployment Name
        azureOpenAIEndpoint,        // Azure OpenAI Endpoint
        azureOpenAIKey              // Azure OpenAI Key
    ).Build();


//Customer Support Supervisor name 
string csSupervisorFirstName = config["MessageReceiverFirstName"];

//Customer Support Agent name 
string csAgentName = config["MessageSenderFullName"];

// Speech Cognitive Service Key for Speech Service 
string speechKey = config["aiservices-key"];

// Azure Region of the Speech Cognitive Service 
string speechRegion = config["SpeechToText:SpeechRegion"];

// File path for the customer support log file
string audioFilePath = config["CallLogFilePath"];

Console.WriteLine($"Converting audio file to text.");

// Create a SpeechToText object, simple and then build out to a fully-functional class
var speechToText = new SpeechToText(speechKey, speechRegion, audioFilePath);

//Get the ResultText variable from the SpeechToText class - this is the text from the call we will use as a prompt for the semantic kernel
string callText = speechToText.getResultText();

//Output the text from the call to the console
Console.WriteLine($"Summary of call from text to speech:" + callText);

//This is the prompt that will be used to extract info from the call text
string summarizePrompt = @"{{$input}} 

Extract the following from the conversation: 
1. Main reason of the conversation 
2. Sentiment of the customer 
3. How did the agent handle the conversation? 
4. What was the final outcome of the conversation 
5. Create a short summary of the conversation
6. You must extract the following information from the phone conversation below: 
a. Call reason (key: reason) 
b. Cause of the incident (key. cause) 
c. Names of all drivers as an array (key: driver_names) 
d. Insurance number (key: insurance_number) 
e. Accident location (key: location) 
f. Car damages as an array (key: damages) 
g. A short, yet detailed summary (key: summary) 

Make sure fields a to g are answered very short, e.g. for location just say the location name. Please answer in JSON machine-readable format, using the keys from above. Format the ouput as JSON object called 'results'. Pretty print the JSON and make sure that is properly closed at the end. ";

//This is the prompt that will be used to extract info from the call and craft an email body
string emailBodyPrompt = @"{{$input}}

Write an email body in HTML format summarizing the call addressed to " + csSupervisorFirstName + " and from the following person " + csAgentName + ".";

var summarize = kernel.CreateFunctionFromPrompt(summarizePrompt);

//Output of summary - this could be saved to a database if you wanted to track summaries only
try
{
    var summaryOutput = await kernel.InvokeAsync(summarize, new() { { "input", callText } });

    Console.WriteLine("Summary of call from file:\"" + audioFilePath+"\"");
    Console.WriteLine();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             

    // Run two prompts in sequence (prompt chaining)
    var emailPromptResults = kernel.CreateFunctionFromPrompt(emailBodyPrompt);

    //need to wait so we don't send too many prompts to the Azure OpenAI service
    Thread.Sleep(60000);

    //Get results from Azure OpenAI summarizing the call as an email with subject and body
    var emailOutput = await kernel.InvokeAsync(emailPromptResults, new() { { "input", summaryOutput.GetValue<string>() } });

    //Space out the results for clarity
    Console.WriteLine("\n");

    //Output of email summary this could be replaced with a call to 
    //the Microsoft Graph to send via Outlook or directly to an SMTP server
    Console.WriteLine("Email summary of call:");
    Console.WriteLine(emailOutput + "\n");

    //Email variables to craft an email and send it
    string connectionString = config["communicationservice-connectionstring"];
    string sender =           config["EmailMessage:SenderEmailAddress"];
    string recipient =        config["EmailMessage:RecieverEmailAddress"];
    string subject =          config["EmailMessage:Subject"];
    string emailBodyTop =     config["EmailMessage:MessageBodyTop"];
    string emailBodyHeader =  config["EmailMessage:MessageBodyHeader"];
    string emailBodyBottom =  config["EmailMessage:MessageBodyBottom"];

    //Create the email body
    string emailContent = emailBodyTop + subject + emailBodyHeader + emailOutput.ToString() + emailBodyBottom;

    //Create SendEmail object and send the email
    var sendEmail = new SendEmail();
    try
    {
        await sendEmail.sendEmailToRecipient(connectionString, sender, recipient, subject, emailContent);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine("Error sending email: " + e.Message);
    }
}
catch (Exception e)
{
    Console.Error.WriteLine("Error summarizing call: " + e.Message);
    return;
}