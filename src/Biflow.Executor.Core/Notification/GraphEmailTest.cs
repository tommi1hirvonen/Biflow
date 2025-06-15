using Biflow.Executor.Core.Notification.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace Biflow.Executor.Core.Notification;

internal class GraphEmailTest(IOptionsMonitor<GraphEmailOptions> optionsMonitor) : IEmailTest
{
    public Task RunAsync(string toAddress)
    {
        var options = optionsMonitor.CurrentValue;
        var fromAddress = options.FromAddress;
        var client = GraphEmailDispatcher.CreateClientFrom(options);
        var recipient = new Recipient
        {
            EmailAddress = new EmailAddress
            {
                Address = toAddress
            }
        };
        var itemBody = new ItemBody
        {
            ContentType = BodyType.Text,
            Content = "This is a test email sent from Biflow."
        };
        var message = new Message
        {
            ToRecipients = [recipient],
            Subject = "Biflow Test Mail",
            Body = itemBody       
        };
        var requestBody = new SendMailPostRequestBody
        {
            Message = message
        };
        return client.Users[fromAddress].SendMail.PostAsync(requestBody);
    }
}