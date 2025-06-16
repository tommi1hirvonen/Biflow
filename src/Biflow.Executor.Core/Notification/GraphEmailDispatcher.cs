using Azure.Core;
using Azure.Identity;
using Biflow.Executor.Core.Notification.Options;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace Biflow.Executor.Core.Notification;

internal class GraphEmailDispatcher(IOptionsMonitor<GraphEmailOptions> optionsMonitor) : IMessageDispatcher
{
    public static GraphServiceClient CreateClientFrom(GraphEmailOptions options)
    {
        TokenCredential credential = options switch
        {
            { UseSystemAssignedManagedIdentity: true } => new ManagedIdentityCredential(),
            { UserAssignedManagedIdentityClientId: { Length: > 0 } id } => new ManagedIdentityCredential(id),
            { ServicePrincipal.TenantId.Length: > 0 } => new ClientSecretCredential(
                options.ServicePrincipal.TenantId,
                options.ServicePrincipal.ClientId,
                options.ServicePrincipal.ClientSecret),
            _ => throw new ArgumentException("Email options could not be converted to a valid TokenCredential.")
        };
        var client = new GraphServiceClient(credential);
        return client;
    }
    
    public Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
        CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        var fromAddress = options.FromAddress;
        var client = CreateClientFrom(options);
        var emailRecipients = recipients
            .Select(x => new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = x
                }
            })
            .ToList();
        var itemBody = new ItemBody
        {
            ContentType = isBodyHtml ? BodyType.Html : BodyType.Text,
            Content = body
        };
        var message = new Message
        {
            BccRecipients = emailRecipients,
            Subject = subject,
            Body = itemBody       
        };
        var requestBody = new SendMailPostRequestBody
        {
            Message = message
        };
        return client.Users[fromAddress].SendMail.PostAsync(requestBody, cancellationToken: cancellationToken);
    }
}