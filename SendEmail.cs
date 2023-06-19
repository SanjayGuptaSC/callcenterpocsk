namespace Callcentersk;

using Azure;
using Azure.Communication.Email;
public class SendEmail
{

 public async Task sendEmailToRecipient(string connectionString, string sender, string recipient, string subject, string emailContent)
    {

        // This code demonstrates how to send email using Azure Communication Services.
        var emailClient = new EmailClient(connectionString);

        try
        {
            var emailSendOperation =  await emailClient.SendAsync(
                wait: WaitUntil.Completed,
                senderAddress: sender, // The email address of the domain registered with the Communication Services resource
                recipientAddress: recipient,
                subject: subject,
                htmlContent: emailContent);
            Console.WriteLine($"Email Sent. Status = {emailSendOperation.Value.Status}");

            /// Get the OperationId so that it can be used for tracking the message for troubleshooting
            string operationId = emailSendOperation.Id;
            Console.WriteLine($"Email operation id = {operationId}");
        }
        catch (RequestFailedException ex)
        {
            /// OperationID is contained in the exception message and can be used for troubleshooting purposes
            Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
        }

    }

}

