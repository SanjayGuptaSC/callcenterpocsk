// This program simulates using Semantic Kernel to extract information from a call center conversation
// and then using that information to craft an email summary of the call.

using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Callcentersk;


//Initialize the kernel
var kernel = Kernel.Builder.Build();

//Call Properties file to get Azure Cognitive Services Speech to Text Service etc.
IConfigurationBuilder configuration = new ConfigurationBuilder().AddJsonFile(path: "appSettings.json", false, true);
IConfigurationRoot configRoot = configuration.Build();

//OpenAI Properties for the Semantic Kernel Service
string azureOpenAIDeploymentName = configRoot.GetSection("AzureOpenAI").GetSection("DeploymentName").Value;
string azureOpenAIEndpoint = configRoot.GetSection("AzureOpenAI").GetSection("Endpoint").Value;
string azureOpenAIKey = configRoot.GetSection("AzureOpenAI").GetSection("ApiKey").Value;
       
//Add Azure Cognitive Services Speech to Text Service
kernel.Config.AddAzureTextCompletionService(
    azureOpenAIDeploymentName,  // Azure OpenAI Deployment Name
    azureOpenAIEndpoint,        // Azure OpenAI Endpoint
    azureOpenAIKey              // Azure OpenAI Key
);

//Customer Support Supervisor name 
string csSupervisorFirstName = configRoot.GetSection("MessageReceiverFirstName").Value;

//Customer Support Agent name 
string csAgentName = configRoot.GetSection("MessageSenderFullName").Value;

// Speech Cognitive Service Key for Speech Service 
string speechKey = configRoot.GetSection("SpeechToText").GetSection("ServiceKey").Value;

// Azure Region of the Speech Cognitive Service 
string speechRegion = configRoot.GetSection("SpeechToText").GetSection("SpeechRegion").Value;

// File path for the customer support log file
string audioFilePath = configRoot.GetSection("CallLogFilePath").Value;

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
5. Create a short summary of the conversation";

//This is the prompt that will be used to extract info from the call and craft an email body
string emailBodyPrompt = @"{{$input}}

Write an email body in HTML format summarizing the call addressed to " + csSupervisorFirstName + " and from the following person " + csAgentName + ".";

var summarize = kernel.CreateSemanticFunction(summarizePrompt);

//Output of summary - this could be saved to a database if you wanted to track summaries only
var summaryOutput = await kernel.RunAsync(callText, summarize);
Console.WriteLine("Summary of call from file:\"" + audioFilePath+"\"");
Console.WriteLine(summaryOutput);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             

// Run two prompts in sequence (prompt chaining)
var emailPromptResults = kernel.CreateSemanticFunction(emailBodyPrompt);

//Get results from Azure OpenAI summarizing the call as an email with subject and body
var emailOutput = await kernel.RunAsync(callText, summarize, emailPromptResults);

//Space out the results for clarity
Console.WriteLine("\n");

//Output of email summary this could be replaced with a call to 
//the Microsoft Graph to send via Outlook or directly to an SMTP server
Console.WriteLine("Email summary of call:");
Console.WriteLine(emailOutput + "\n");

//Email variables to craft an email and send it
string connectionString = configRoot.GetSection("EmailServiceConnectionString").Value;
string sender = configRoot.GetSection("EmailMessage").GetSection("SenderEmailAddress").Value;
string recipient = configRoot.GetSection("EmailMessage").GetSection("RecieverEmailAddress").Value;
string subject = configRoot.GetSection("EmailMessage").GetSection("Subject").Value;
string emailBodyTop = configRoot.GetSection("EmailMessage").GetSection("MessageBodyTop").Value;
string emailBodyHeader = configRoot.GetSection("EmailMessage").GetSection("MessageBodyHeader").Value;
string emailBodyBottom = configRoot.GetSection("EmailMessage").GetSection("MessageBodyBottom").Value;

//Create the email body
string emailContent = emailBodyTop + subject + emailBodyHeader + emailOutput.ToString() + emailBodyBottom;

//Create SendEmail object and send the email
var sendEmail = new SendEmail();
try{
    await sendEmail.sendEmailToRecipient(connectionString, sender, recipient, subject, emailContent);
}
catch (Exception e)
{
    Console.WriteLine("Error sending email: " + e.Message);
}
